"""
Main A2A server application for the Python financial analysis agent.
Uses the official A2A Python SDK with FastAPI and JSON-RPC support.
"""
import os
import logging

import click
import uvicorn

# A2A SDK imports
from a2a.server.apps import A2AFastAPIApplication
from a2a.server.request_handlers import DefaultRequestHandler
from a2a.server.tasks import InMemoryTaskStore
from a2a.types import (
    AgentCapabilities,
    AgentCard,
    AgentSkill,
    TransportProtocol,
)

# OpenTelemetry imports
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor

# Microsoft Agent Framework
from agent_framework.observability import setup_observability

# Local imports
from .agent_executor import FinancialAnalysisAgentExecutor

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def get_agent_card(host: str, port: int) -> AgentCard:
    """
    Create and return the AgentCard for the financial analysis agent.
    
    Args:
        host: The hostname where the agent is running
        port: The port number where the agent is running
        
    Returns:
        AgentCard: The agent card describing capabilities and skills
    """
    return AgentCard(
        name="financial-analysis-agent",
        description="Python-based agent for financial analysis, reporting, and business metrics using Microsoft Agent Framework",
        url=f"http://localhost:{port}/",
        version="1.0.0",
        default_input_modes=["text"],
        default_output_modes=["text"],
        capabilities=AgentCapabilities(
            streaming=True,
            push_notifications=False
        ),
        preferred_transport=TransportProtocol.http_json,
        skills=[
            AgentSkill(
                id="sales-data-analysis",
                name="Sales Data Analysis",
                description="Search and analyze sales data with filtering by date range, category, and other criteria",
                examples=[
                    "Show me sales data for the last quarter",
                    "What were our top-selling products in electronics last month?",
                    "Get sales performance for Q3 2024"
                ],
                tags=["sales", "data-analysis", "reporting"]
            ),
            AgentSkill(
                id="revenue-trend-analysis",
                name="Revenue Trend Analysis",
                description="Analyze revenue trends and generate growth forecasts with detailed insights",
                examples=[
                    "Analyze revenue trends for the past year",
                    "What's our revenue growth trajectory?", 
                    "Show me monthly revenue patterns"
                ],
                tags=["revenue", "trends", "forecasting", "analysis"]
            ),
            AgentSkill(
                id="business-metrics-calculation",
                name="Business Metrics Calculation",
                description="Calculate key business metrics like profit margins, growth rates, and performance indicators",
                examples=[
                    "Calculate our profit margin for Q4",
                    "What's our revenue growth rate?",
                    "Show me customer acquisition cost metrics"
                ],
                tags=["metrics", "calculations", "kpi", "business-intelligence"]
            ),
            AgentSkill(
                id="top-products-analysis",
                name="Top Products Analysis",
                description="Identify and analyze top-performing products by various metrics and time periods",
                examples=[
                    "What are our top 10 products by revenue?",
                    "Show me best-selling products last quarter",
                    "Which products have the highest profit margins?"
                ],
                tags=["products", "performance", "ranking", "analysis"]
            ),
            AgentSkill(
                id="customer-analytics",
                name="Customer Analytics",
                description="Perform customer segmentation, lifetime value analysis, and churn prediction",
                examples=[
                    "Analyze customer segments by purchase behavior",
                    "Calculate customer lifetime value",
                    "What's our customer retention rate?"
                ],
                tags=["customers", "segmentation", "lifetime-value", "churn", "analytics"]
            ),
            AgentSkill(
                id="financial-reporting",
                name="Financial Reporting",
                description="Generate comprehensive financial reports and executive summaries",
                examples=[
                    "Generate a financial summary for the board",
                    "Create a Q4 revenue report",
                    "Prepare executive dashboard metrics"
                ],
                tags=["reporting", "financial", "executive", "dashboard"]
            ),
            AgentSkill(
                id="data-processing",
                name="Data Processing",
                description="Process and analyze CSV/Excel files, validate financial data, and perform statistical analysis",
                examples=[
                    "Parse this sales data CSV file",
                    "Validate financial data consistency",
                    "Perform statistical analysis on revenue data"
                ],
                tags=["data-processing", "csv", "excel", "validation", "statistics"]
            )
        ]
    )

def main():
    """Main entry point for the application (environment-based configuration)."""
    port = int(os.environ.get("PORT", 8001))
    host = os.environ.get("HOST", "0.0.0.0")
    
    logger.info(f"Server starting on http://{host}:{port}")
    logger.info(f"Environment PORT={os.environ.get('PORT', 'not set')}")
    logger.info(f"Environment HOST={os.environ.get('HOST', 'not set')}")
    
    # Setup observability
    setup_observability()
    
    # Create agent card
    agent_card = get_agent_card(host, port)
    
    # Create agent executor
    agent_executor = FinancialAnalysisAgentExecutor()
    
    # Create task store
    task_store = InMemoryTaskStore()
    
    # Create request handler
    http_handler = DefaultRequestHandler(
        agent_executor=agent_executor,
        task_store=task_store,
    )
    
    # Create A2A server application using FastAPI with JSON-RPC support
    server = A2AFastAPIApplication(
        agent_card=agent_card, 
        http_handler=http_handler
    )
    
    # Build the FastAPI app
    app_instance = server.build()

    trace.set_tracer_provider(TracerProvider())
    otlpExporter = OTLPSpanExporter(endpoint=os.environ.get("OTEL_EXPORTER_OTLP_ENDPOINT"))
    processor = BatchSpanProcessor(otlpExporter)
    trace.get_tracer_provider().add_span_processor(processor)

    FastAPIInstrumentor().instrument_app(app_instance)
    
    logger.info(f"Agent Card available at: http://{host}:{port}/.well-known/agent.json")
    logger.info(f"JSON-RPC endpoint: POST http://{host}:{port}/")
    logger.info("Server configured with A2A SDK and FastAPI")
    
    # Start the server
    uvicorn.run(
        app_instance, 
        host=host, 
        port=port,
        log_level="info"
    )


if __name__ == "__main__":
    main()