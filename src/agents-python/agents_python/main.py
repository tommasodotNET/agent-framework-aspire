"""
Main FastAPI application for the Python financial analysis agent using Microsoft Agent Framework.
"""
import os
import asyncio
import json
from typing import AsyncGenerator, Dict, Any, List, Annotated
from decimal import Decimal

import uvicorn
from fastapi import FastAPI, Depends, HTTPException
from fastapi.responses import StreamingResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import Field

# OpenTelemetry imports
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor

# Microsoft Agent Framework
from agent_framework import ChatAgent
from agent_framework.observability import get_tracer, setup_observability
from agent_framework.azure import AzureOpenAIChatClient
from azure.identity import DefaultAzureCredential

from .models import AIChatRequest, AIChatCompletionDelta, AIChatMessageDelta
from .a2a_models import AgentCard, AgentCapabilities, AgentSkill, JsonRpcRequest, JsonRpcResponse, AgentMessageResponse, MessagePart
from services.financial_service import FinancialService
from tools.financial_tools import FinancialTools
from tools import financial_processing_tools

# Initialize financial services and tools
financial_service = FinancialService()
financial_tools = FinancialTools(financial_service)

# Initialize the financial analysis agent
def create_financial_agent() -> ChatAgent:
    """Create and configure the ChatAgent with financial analysis capabilities."""
    try:
        agent = ChatAgent(
            chat_client=AzureOpenAIChatClient(credential=DefaultAzureCredential()),
            instructions="""You are a specialized Financial Analysis and Business Intelligence Assistant. Your role is to help users analyze financial data, calculate business metrics, and generate insights for strategic decision-making.

Your capabilities include:
- Sales data analysis and product performance evaluation
- Revenue trend analysis and growth forecasting  
- Customer acquisition cost calculations and lifetime value analysis
- Employee performance metrics and payroll analytics
- Business intelligence reporting and executive summaries
- Financial KPI tracking and benchmarking against industry standards

When users ask about financial metrics, always provide specific calculations, trends, and actionable insights. For business questions, explain the methodology used and highlight key findings. Be thorough and data-driven in your responses while making complex financial concepts accessible.

Sample areas you can help with:
- "What were our top-performing products last quarter?"
- "Calculate our customer acquisition cost by segment"  
- "Show me revenue trends and growth projections"
- "Generate an executive financial summary for the board"
- "Analyze employee performance metrics and compensation effectiveness"
- "What's our customer lifetime value and churn risk analysis?"

Always focus on providing actionable business insights based on the available financial data.""",
            tools=[
                # FinancialTools class methods (business intelligence)
                financial_tools.search_sales_data,
                financial_tools.analyze_revenue_trends,
                financial_tools.calculate_business_metrics,
                financial_tools.get_top_performing_products,
                financial_tools.analyze_customer_metrics,
                financial_tools.get_financial_summary,
                
                # Financial processing tools (file processing, database, reports)
                financial_processing_tools.parse_csv_file,
                financial_processing_tools.parse_excel_file,
                financial_processing_tools.query_database,
                financial_processing_tools.generate_financial_report,
                financial_processing_tools.validate_financial_data,
                financial_processing_tools.perform_statistical_analysis,
            ]
        )
        return agent
    except Exception as e:
        print(f"Warning: Could not initialize Azure ChatAgent: {e}")
        return None

# Initialize the agent
financial_agent = create_financial_agent()


def create_app() -> FastAPI:
    """Create and configure the FastAPI application."""
    
    app = FastAPI(
        title="Financial Analysis Agent API",
        description="Python-based agent for financial analysis, reporting, and business metrics using Microsoft Agent Framework",
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


app = create_app()

async def generate_streaming_response(content: str) -> AsyncGenerator[str, None]:
    """Generate a streaming response from content."""
    # Split content into chunks for streaming
    words = content.split()
    
    for i, word in enumerate(words):
        # Create a delta message for each word
        message_delta = AIChatMessageDelta(content=word + (" " if i < len(words) - 1 else ""))
        delta = AIChatCompletionDelta(delta=message_delta)
        
        # Yield the JSON-serialized delta followed by newline
        yield f"{delta.model_dump_json(by_alias=True)}\r\n"
        
        # Add a small delay to simulate realistic streaming
        await asyncio.sleep(0.05)

@app.post("/agent/chat/stream")
async def chat_stream(request: AIChatRequest):
    """
    Handle chat requests and return streaming responses with financial analysis capabilities.
    
    This endpoint uses Microsoft Agent Framework when available, with fallback to direct tools.
    """ 
    if request.messages:
        last_message = request.messages[-1]
    
    async def stream_generator():
        # Handle empty messages with introduction
        if not request.messages:
            intro_message = (
                "Hi, I'm the Python Financial Analysis Agent! 🔢"
            )
            async for chunk in generate_streaming_response(intro_message):
                yield chunk
            return
        
        # Process user query with financial analysis
        last_message = request.messages[-1]
        
        # Generate contextual financial response using agent or fallback
        response_content = await financial_agent.run(last_message.content)
        
        # # Stream the response
        async for chunk in generate_streaming_response(response_content.text):
            yield chunk
    
    return StreamingResponse(
        stream_generator(),
        media_type="text/plain",
        headers={
            "Cache-Control": "no-cache",
            "Connection": "keep-alive",
            "X-Accel-Buffering": "no",  # Disable nginx buffering
        }
    )


@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {
        "status": "healthy", 
        "agent": "python-financial-agent", 
        "version": "1.0.0",
        "agent_framework": "Microsoft Agent Framework" if financial_agent else "Direct Tools"
    }


# A2A (Agent-to-Agent) Protocol Endpoints
@app.get("/agenta2a/v1/card", response_model=AgentCard)
async def get_agent_card():
    """
    Returns the agent card for A2A (Agent-to-Agent) communication.
    This endpoint describes the agent's capabilities, skills, and metadata.
    """
    port = int(os.environ.get("PORT", 8001))
    base_url = f"http://localhost:{port}"
    
    agent_card = AgentCard(
        Name="financial-analysis-agent",
        Url=f"{base_url}/agenta2a",
        Description="Python-based agent for financial analysis, reporting, and business metrics using Microsoft Agent Framework",
        Version="1.0.0",
        ProtocolVersion="1.0",
        DefaultInputModes=["text"],
        DefaultOutputModes=["text"],
        Capabilities=AgentCapabilities(
            Streaming=True,
            PushNotifications=False
        ),
        Skills=[
            AgentSkill(
                Id="sales-data-analysis",
                Name="Sales Data Analysis",
                Description="Search and analyze sales data with filtering by date range, category, and other criteria",
                Examples=[
                    "Show me sales data for the last quarter",
                    "What were our top-selling products in electronics last month?",
                    "Get sales performance for Q3 2024"
                ],
                Tags=["sales", "data-analysis", "reporting"]
            ),
            AgentSkill(
                Id="revenue-trend-analysis",
                Name="Revenue Trend Analysis",
                Description="Analyze revenue trends and generate growth forecasts with detailed insights",
                Examples=[
                    "Analyze revenue trends for the past year",
                    "What's our revenue growth trajectory?", 
                    "Show me monthly revenue patterns"
                ],
                Tags=["revenue", "trends", "forecasting", "analysis"]
            ),
            AgentSkill(
                Id="business-metrics-calculation",
                Name="Business Metrics Calculation",
                Description="Calculate key business metrics like profit margins, growth rates, and performance indicators",
                Examples=[
                    "Calculate our profit margin for Q4",
                    "What's our revenue growth rate?",
                    "Show me customer acquisition cost metrics"
                ],
                Tags=["metrics", "calculations", "kpi", "business-intelligence"]
            ),
            AgentSkill(
                Id="top-products-analysis",
                Name="Top Products Analysis",
                Description="Identify and analyze top-performing products by various metrics and time periods",
                Examples=[
                    "What are our top 10 products by revenue?",
                    "Show me best-selling products last quarter",
                    "Which products have the highest profit margins?"
                ],
                Tags=["products", "performance", "ranking", "analysis"]
            ),
            AgentSkill(
                Id="customer-analytics",
                Name="Customer Analytics",
                Description="Perform customer segmentation, lifetime value analysis, and churn prediction",
                Examples=[
                    "Analyze customer segments by purchase behavior",
                    "Calculate customer lifetime value",
                    "What's our customer retention rate?"
                ],
                Tags=["customers", "segmentation", "lifetime-value", "churn", "analytics"]
            ),
            AgentSkill(
                Id="financial-reporting",
                Name="Financial Reporting",
                Description="Generate comprehensive financial reports and executive summaries",
                Examples=[
                    "Generate a financial summary for the board",
                    "Create a Q4 revenue report",
                    "Prepare executive dashboard metrics"
                ],
                Tags=["reporting", "financial", "executive", "dashboard"]
            ),
            AgentSkill(
                Id="data-processing",
                Name="Data Processing",
                Description="Process and analyze CSV/Excel files, validate financial data, and perform statistical analysis",
                Examples=[
                    "Parse this sales data CSV file",
                    "Validate financial data consistency",
                    "Perform statistical analysis on revenue data"
                ],
                Tags=["data-processing", "csv", "excel", "validation", "statistics"]
            )
        ]
    )
    
    return agent_card


@app.post("/agenta2a")
async def a2a_json_rpc_endpoint(request: dict):
    """
    A2A JSON-RPC 2.0 endpoint for agent-to-agent communication.
    Handles message/send method calls from other agents.
    """
    import uuid
    from datetime import datetime
    
    # Log the incoming request for debugging
    print(f"Received A2A request: {request}")
    
    try:
        # Handle JSON-RPC 2.0 format
        if "jsonrpc" in request and "method" in request and request["method"] == "message/send":
            # Extract message from JSON-RPC params
            params = request.get("params", {})
            message = params.get("message", {})
            parts = message.get("parts", [])
            
            # Extract text content from parts
            text_content = ""
            for part in parts:
                if part.get("kind") == "text" and part.get("text"):
                    text_content += part["text"] + " "
            text_content = text_content.strip()
            
            if not text_content:
                text_content = "Hello, I'm ready to help with financial analysis!"
                
            original_message_id = message.get("messageId")
            
        else:
            # Fallback for other formats
            if "Message" in request:
                text_content = request["Message"].get("Content", "")
            elif "message" in request:
                text_content = request["message"].get("content", "")
            else:
                text_content = "Hello, I'm ready to help with financial analysis!"
            original_message_id = None
        
        # Generate a unique message ID
        message_id = str(uuid.uuid4())
        
        # Process the message using our financial agent
        if financial_agent and text_content:
            try:
                response_content = await financial_agent.run(text_content)
                content = response_content.text
            except Exception as e:
                print(f"Agent processing error: {e}")
                content = f"I received your question: '{text_content}'. I'm a financial analysis agent ready to help with business metrics, sales data, and financial reporting."
        else:
            # Fallback response if agent framework isn't available
            content = f"I received your question: '{text_content}'. I'm a financial analysis agent ready to help with business metrics, sales data, and financial reporting."
        
        # Return JSON-RPC 2.0 response if it was a JSON-RPC request
        if "jsonrpc" in request:
            # Create the AgentMessage response structure
            agent_message = AgentMessageResponse(
                kind="message",
                role="agent",
                parts=[MessagePart(kind="text", text=content)],
                messageId=message_id,
                contextId=None,
                taskId=None,
                referenceTaskIds=None,
                extensions=None,
                metadata={
                    "timestamp": datetime.utcnow().isoformat(),
                    "agent": "python-financial-agent",
                    "version": "1.0.0",
                    "original_message_id": original_message_id
                }
            )
            
            response = {
                "jsonrpc": "2.0",
                "id": request.get("id"),
                "result": agent_message.model_dump(by_alias=True)
            }
        else:
            # Return simple response for non-JSON-RPC requests (also in correct format)
            response = {
                "kind": "message",
                "role": "agent",
                "parts": [{"kind": "text", "text": content}],
                "messageId": message_id,
                "contextId": None,
                "taskId": None,
                "referenceTaskIds": None,
                "extensions": None,
                "metadata": {
                    "timestamp": datetime.utcnow().isoformat(),
                    "agent": "python-financial-agent",
                    "version": "1.0.0"
                }
            }
        
        print(f"Sending A2A response: {response}")
        return response
        
    except Exception as e:
        print(f"Error processing A2A request: {e}")
        import traceback
        traceback.print_exc()
        
        # Return JSON-RPC error if it was a JSON-RPC request
        if "jsonrpc" in request:
            return {
                "jsonrpc": "2.0",
                "id": request.get("id", "unknown"),
                "error": {
                    "code": -32603,
                    "message": "Internal error",
                    "data": str(e)
                }
            }
        else:
            # Return simple error response in AgentMessage format
            return {
                "kind": "message",
                "role": "agent",
                "parts": [{"kind": "text", "text": "I encountered an error processing your request, but I'm a financial analysis agent ready to help!"}],
                "messageId": str(uuid.uuid4()),
                "contextId": None,
                "taskId": None,
                "referenceTaskIds": None,
                "extensions": None,
                "metadata": {
                    "error": str(e),
                    "timestamp": datetime.utcnow().isoformat(),
                    "agent": "python-financial-agent",
                    "version": "1.0.0"
                }
            }


# Direct API endpoints for specific financial operations
@app.get("/api/tools/sales")
async def get_sales_data(
    query: str = "all",
    date_range: str = "last_quarter", 
    category: str = None
):
    """Direct endpoint for sales data retrieval."""
    try:
        result = await financial_tools.search_sales_data(query, date_range, category)
        return {"data": json.loads(result)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error retrieving sales data: {str(e)}")


@app.get("/api/tools/metrics")
async def calculate_metrics(
    metrics: str = "revenue_growth_rate,profit_margin",
    period: str = "current"
):
    """Direct endpoint for business metrics calculation."""
    try:
        metric_list = metrics.split(",")
        result = await financial_tools.calculate_business_metrics(metric_list, period)
        return {"data": json.loads(result)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error calculating metrics: {str(e)}")


@app.get("/api/tools/top-products") 
async def get_top_products(
    time_period: str = "last_quarter",
    metric: str = "revenue",
    limit: int = 10
):
    """Direct endpoint for top performing products."""
    try:
        result = await financial_tools.get_top_performing_products(time_period, metric, limit)
        return {"data": json.loads(result)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error retrieving top products: {str(e)}")


@app.get("/api/tools/customer-analysis")
async def analyze_customers(
    analysis_type: str = "segmentation",
    segment: str = None,
    include_predictions: bool = False
):
    """Direct endpoint for customer analysis."""
    try:
        result = await financial_tools.analyze_customer_metrics(analysis_type, segment, include_predictions)
        return {"data": json.loads(result)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error analyzing customers: {str(e)}")


def main():
    # This will enable tracing and create the necessary tracing, logging and metrics providers
    # based on environment variables. See the .env.example file for the available configuration options.
    setup_observability()

    """Main entry point for the application."""
    port = int(os.environ.get("PORT", 8001))
    host = os.environ.get("HOST", "0.0.0.0")
    
    print(f"Starting Financial Analysis Agent...")
    print(f"Agent Framework: {'Microsoft Agent Framework' if financial_agent else 'Direct Tools Mode'}")
    
    uvicorn.run(
        "agents_python.main:app",
        host=host,
        port=port,
        reload=True,
        access_log=True,
        log_level="info",
    )


if __name__ == "__main__":
    main()