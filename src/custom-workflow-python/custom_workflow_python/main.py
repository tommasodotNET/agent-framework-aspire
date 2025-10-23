# Copyright (c) Microsoft. All rights reserved.

import asyncio
import logging
import os
from typing import Any, Dict, List, Optional

import httpx
from a2a.client import A2ACardResolver
from agent_framework import (
    AgentExecutor,
    AgentExecutorRequest,
    AgentExecutorResponse,
    AgentRunResponse,
    ChatMessage,
    Role,
    WorkflowBuilder,
    WorkflowContext,
    executor,
)
from agent_framework.a2a import A2AAgent
from agent_framework.observability import get_tracer, setup_observability
from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from pydantic import BaseModel, Field
from typing_extensions import Never
import uvicorn

# Load environment variables
load_dotenv()

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

"""
Custom Workflow: Financial Analysis with Policy Compliance

This workflow demonstrates:
1. Financial analysis agent that analyzes Q4 sales performance
2. Policy lookup agent that finds relevant compliance policies
3. Conditional routing based on financial analysis results
4. Final compliance report generation

Flow:
1. Financial agent analyzes sales data and returns FinancialAnalysisResult
2. If policy compliance check is needed, route to policy agent
3. Policy agent looks up relevant policies and returns PolicyLookupResult
4. Generate final compliance report combining both results
"""

# Data Models for structured responses
class FinancialAnalysisResult(BaseModel):
    """Result from financial analysis agent."""
    
    # Core financial metrics
    total_revenue: float = Field(description="Total revenue for the period")
    top_products: List[str] = Field(description="List of top performing products")
    revenue_growth_rate: float = Field(description="Revenue growth rate percentage")
    profit_margin: float = Field(description="Overall profit margin percentage")
    
    # Analysis flags
    needs_policy_review: bool = Field(description="Whether policy compliance review is needed")
    risk_areas: List[str] = Field(description="Areas that may need policy review")
    
    # Original query context
    query_context: str = Field(description="Original analysis query for downstream agents")
    analysis_period: str = Field(description="Time period analyzed (e.g., Q4 2024)")


class PolicyLookupResult(BaseModel):
    """Result from policy lookup agent."""
    
    # Policy information
    relevant_policies: List[str] = Field(description="List of relevant policy names")
    compliance_requirements: List[str] = Field(description="Key compliance requirements")
    risk_mitigation_steps: List[str] = Field(description="Recommended risk mitigation steps")
    
    # Policy metadata
    policy_categories: List[str] = Field(description="Categories of policies found")
    last_updated: str = Field(description="When policies were last updated")
    
    # Context from financial analysis
    financial_context: str = Field(description="Financial analysis context that triggered policy lookup")


class ComplianceReport(BaseModel):
    """Final compliance report combining financial analysis and policy lookup."""
    
    # Executive summary
    executive_summary: str = Field(description="High-level summary of findings")
    
    # Financial insights
    financial_highlights: Dict[str, Any] = Field(description="Key financial metrics and insights")
    
    # Compliance status
    compliance_status: str = Field(description="Overall compliance status (compliant/needs-attention/non-compliant)")
    policy_alignment: List[str] = Field(description="How financial results align with policies")
    
    # Recommendations
    action_items: List[str] = Field(description="Recommended actions for compliance")
    priority_areas: List[str] = Field(description="High-priority areas requiring attention")


# Condition function for routing logic
def get_policy_review_condition(expected_review_needed: bool):
    """Create a condition that routes based on whether policy review is needed."""
    
    def condition(message: Any) -> bool:
        # Defensive guard
        if not isinstance(message, AgentExecutorResponse):
            return True
        
        try:
            # Get the response text from the agent
            response_text = message.agent_run_response.text.lower()
            
            # Look for keywords that indicate policy review might be needed
            policy_keywords = [
                "compliance", "policy", "regulation", "risk", "audit", 
                "legal", "governance", "sox", "asc 606", "revenue recognition",
                "commission", "international", "regulatory", "standards"
            ]
            
            # Check if any policy-related keywords are mentioned
            needs_review = any(keyword in response_text for keyword in policy_keywords)
            
            # Route based on whether policy review is needed
            return needs_review == expected_review_needed
            
        except Exception as e:
            logger.warning(f"Failed to analyze financial response for routing: {e}")
            # Default to not needing policy review if we can't determine
            return expected_review_needed == False
    
    return condition


# Executor functions
@executor(id="handle_compliance_report")
async def handle_compliance_report(
    response: AgentExecutorResponse, 
    ctx: WorkflowContext[Never, str]
) -> None:
    """Handle final compliance report and yield workflow output."""
    try:
        # Get the compliance report response directly
        compliance_report = response.agent_run_response.text
        
        # Format the output for the user
        output = f"""
FINANCIAL ANALYSIS & COMPLIANCE REPORT
====================================

{compliance_report}
"""
        
        await ctx.yield_output(output.strip())
        
    except Exception as e:
        logger.error(f"Error handling compliance report: {e}")
        await ctx.yield_output(f"Error generating compliance report: {str(e)}")


@executor(id="handle_financial_only")
async def handle_financial_only(
    response: AgentExecutorResponse,
    ctx: WorkflowContext[Never, str]
) -> None:
    """Handle financial analysis when no policy review is needed."""
    try:
        # Get the financial analysis response directly
        financial_analysis = response.agent_run_response.text
        
        output = f"""
FINANCIAL ANALYSIS REPORT
========================

{financial_analysis}

STATUS: No policy compliance review required.
"""
        
        await ctx.yield_output(output.strip())
        
    except Exception as e:
        logger.error(f"Error handling financial analysis: {e}")
        await ctx.yield_output(f"Error processing financial analysis: {str(e)}")


@executor(id="to_policy_lookup_request")
async def to_policy_lookup_request(
    response: AgentExecutorResponse,
    ctx: WorkflowContext[AgentExecutorRequest]
) -> None:
    """Transform financial analysis result into a policy lookup request."""
    try:
        # Extract the financial analysis response text
        financial_analysis = response.agent_run_response.text
        
        # Create a policy lookup request based on the financial analysis
        policy_query = f"""
Based on the following financial analysis:

{financial_analysis}

Please find relevant policies that apply to the financial metrics, products, and business areas mentioned above. Focus on:
- Revenue recognition policies
- Sales commission policies
- International sales compliance
- Financial reporting standards
- Any regulatory requirements mentioned

Provide specific policy names and compliance requirements.
"""
        
        user_msg = ChatMessage(Role.USER, text=policy_query.strip())
        await ctx.send_message(AgentExecutorRequest(messages=[user_msg], should_respond=True))
        
    except Exception as e:
        logger.error(f"Error transforming to policy lookup request: {e}")
        # Send a fallback request
        fallback_msg = ChatMessage(Role.USER, text="Please lookup general sales and revenue recognition policies.")
        await ctx.send_message(AgentExecutorRequest(messages=[fallback_msg], should_respond=True))


@executor(id="to_compliance_report_request")
async def to_compliance_report_request(
    response: AgentExecutorResponse,
    ctx: WorkflowContext[AgentExecutorRequest]
) -> None:
    """Transform policy lookup result into a compliance report generation request."""
    try:
        # Extract the policy lookup response text
        policy_analysis = response.agent_run_response.text
        
        compliance_query = f"""
Generate a comprehensive compliance report based on the following policy analysis:

{policy_analysis}

Please create a compliance report that includes:
- Executive summary of compliance status
- Key policy findings and requirements
- Risk assessment and mitigation recommendations
- Action items for maintaining compliance
- Overall compliance status (compliant/needs-attention/non-compliant)

Format the response as a clear, executive-level compliance report.
"""
        
        user_msg = ChatMessage(Role.USER, text=compliance_query.strip())
        await ctx.send_message(AgentExecutorRequest(messages=[user_msg], should_respond=True))
        
    except Exception as e:
        logger.error(f"Error transforming to compliance report request: {e}")
        # Send a fallback request
        fallback_msg = ChatMessage(Role.USER, text="Please generate a basic compliance report.")
        await ctx.send_message(AgentExecutorRequest(messages=[fallback_msg], should_respond=True))


# A2A Agent creation functions
async def create_financial_agent(http_client: httpx.AsyncClient) -> A2AAgent:
    """Create a financial analysis A2A agent (connects to Python agent service)."""
    financial_agent_host = os.getenv("services__pythonagent__http__0")
    
    resolver = A2ACardResolver(httpx_client=http_client, base_url=financial_agent_host)
    agent_card = await resolver.get_agent_card(relative_card_path="/.well-known/agent-card.json")
    
    return A2AAgent(
        name=agent_card.name or "Financial Analysis Agent",
        description=agent_card.description or "Analyzes financial data and business metrics",
        agent_card=agent_card,
        url=financial_agent_host,
    )


async def create_policy_agent(http_client: httpx.AsyncClient) -> A2AAgent:
    """Create a policy/document lookup A2A agent (connects to .NET agent service)."""
    policy_agent_host = os.getenv("services__dotnetagent__http__0")
    
    resolver = A2ACardResolver(httpx_client=http_client, base_url=policy_agent_host)
    agent_card = await resolver.get_agent_card(relative_card_path="/agenta2a/v1/card")
    
    return A2AAgent(
        name=agent_card.name or "Document/Policy Lookup Agent",
        description=agent_card.description or "Searches and retrieves company policies and documents",
        agent_card=agent_card,
        url=policy_agent_host,
    )


@executor(id="mock_compliance_agent")
async def mock_compliance_agent(
    request: AgentExecutorRequest,
    ctx: WorkflowContext[Never, str]
) -> None:
    """Mock compliance agent executor that always returns positive compliance status."""
    try:
        # Create a mock compliance report
        compliance_report = ComplianceReport(
            executive_summary="All financial activities are in full compliance with company policies and regulatory requirements. No issues detected.",
            financial_highlights={
                "Compliance Score": "100%",
                "Risk Level": "Low",
                "Last Audit": "2024-Q4",
                "Status": "Fully Compliant"
            },
            compliance_status="compliant",
            policy_alignment=[
                "Revenue recognition practices fully align with ASC 606 standards",
                "Sales commission structures meet all regulatory requirements",
                "International sales comply with all applicable jurisdictions",
                "Financial reporting meets SOX compliance standards"
            ],
            action_items=[
                "Continue quarterly compliance monitoring",
                "Maintain current best practices",
                "Schedule next routine audit for Q1 2025"
            ],
            priority_areas=["Routine Monitoring", "Best Practice Maintenance"]
        )
        
        # Generate the compliance report text directly
        compliance_report_text = f"""
FINANCIAL ANALYSIS & COMPLIANCE REPORT
====================================

EXECUTIVE SUMMARY:
All financial activities are in full compliance with company policies and regulatory requirements. No issues detected.

FINANCIAL HIGHLIGHTS:
• Compliance Score: 100%
• Risk Level: Low
• Last Audit: 2024-Q4
• Status: Fully Compliant

COMPLIANCE STATUS: COMPLIANT

POLICY ALIGNMENT:
• Revenue recognition practices fully align with ASC 606 standards
• Sales commission structures meet all regulatory requirements
• International sales comply with all applicable jurisdictions
• Financial reporting meets SOX compliance standards

ACTION ITEMS:
• Continue quarterly compliance monitoring
• Maintain current best practices
• Schedule next routine audit for Q1 2025

PRIORITY AREAS:
• Routine Monitoring
• Best Practice Maintenance
"""
        
        # Yield the output directly
        await ctx.yield_output(compliance_report_text.strip())
        
    except Exception as e:
        logger.error(f"Error in mock compliance agent: {e}")
        await ctx.yield_output(f"Error generating compliance report: {str(e)}")


# Mock classes removed - using executor function approach instead


# Workflow builder function
async def create_workflow_with_client(http_client: httpx.AsyncClient) -> Any:
    """Create and configure the financial compliance workflow."""
    # Create A2A agents
    financial_agent = await create_financial_agent(http_client)
    policy_agent = await create_policy_agent(http_client)
    
    # Wrap A2A agents in AgentExecutors
    financial_executor = AgentExecutor(financial_agent, id="financial_analysis_agent")
    policy_executor = AgentExecutor(policy_agent, id="policy_lookup_agent")
    # Note: compliance_executor is now the mock_compliance_agent executor function
    
    # Build the workflow graph
    workflow = (
        WorkflowBuilder()
        .set_start_executor(financial_executor)
        
        # Policy review path: financial -> policy lookup -> compliance report -> output
        .add_edge(financial_executor, to_policy_lookup_request, condition=get_policy_review_condition(True))
        .add_edge(to_policy_lookup_request, policy_executor)
        .add_edge(policy_executor, to_compliance_report_request)
        .add_edge(to_compliance_report_request, mock_compliance_agent)
        
        # No policy review path: financial -> direct output
        .add_edge(financial_executor, handle_financial_only, condition=get_policy_review_condition(False))
        
        .build()
    )
    
    return workflow


def create_app() -> FastAPI:
    """Create and configure the FastAPI application."""
    
    app = FastAPI(
        title="Financial Compliance Workflow API",
        description="API for financial analysis with policy compliance checking using Microsoft Agent Framework",
        version="1.0.0",
    )
    
    # Add CORS middleware to allow frontend requests
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],  # In production, replace with specific origins
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )
    
    # Instrument FastAPI with OpenTelemetry
    trace.set_tracer_provider(TracerProvider())
    otlpExporter = OTLPSpanExporter(endpoint=os.environ.get("OTEL_EXPORTER_OTLP_ENDPOINT"))
    processor = BatchSpanProcessor(otlpExporter)
    trace.get_tracer_provider().add_span_processor(processor)

    FastAPIInstrumentor().instrument_app(app)
    
    return app


# FastAPI application
app = create_app()


@app.get("/analyze")
async def analyze_financial_compliance():
    """Analyze financial data with policy compliance checking (hardcoded example)."""
    try:
        # Create workflow with HTTP client
        async with httpx.AsyncClient(timeout=60.0) as http_client:
            workflow = await create_workflow_with_client(http_client)
            
            # Hardcoded analysis request
            analysis_query = """
Analyze financial performance for Q4 2024:
I need to analyze our Q4 sales performance for our enterprise software products, but I also need to ensure we're compliant with our sales commission and revenue recognition policies. Can you provide both the financial analysis and the relevant policy requirements?

Focus on enterprise software products and include:
- Revenue metrics and growth rates
- Top performing products
- Profit margins and trends
- Any areas that might require policy compliance review
"""
            
            # Execute workflow
            executor_request = AgentExecutorRequest(
                messages=[ChatMessage(Role.USER, text=analysis_query.strip())],
                should_respond=True
            )
            
            events = await workflow.run(executor_request)
            outputs = events.get_outputs()
            
            if outputs:
                return {
                    "status": "success",
                    "result": outputs[0],
                    "query": "Q4 2024 enterprise software sales analysis with policy compliance"
                }
            else:
                return {
                    "status": "error",
                    "result": "No output generated from workflow"
                }
                
    except Exception as e:
        logger.error(f"Error in financial compliance analysis: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy", "service": "financial-compliance-workflow"}


def main():
    # This will enable tracing and create the necessary tracing, logging and metrics providers
    # based on environment variables. See the .env.example file for the available configuration options.
    setup_observability()

    """Main function to run the FastAPI application."""
    port = int(os.environ.get("PORT", 8001))
    host = os.environ.get("HOST", "0.0.0.0")
    
    uvicorn.run(
        "custom_workflow_python.main:app",
        host=host,
        port=port,
        reload=False,
        access_log=True,
        log_level="info"
    )


if __name__ == "__main__":
    main()
