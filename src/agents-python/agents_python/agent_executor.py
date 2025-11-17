"""
Financial Analysis Agent Executor for A2A SDK.
This module implements the AgentExecutor interface for financial analysis operations.
"""
import logging
from typing import override

from a2a.server.agent_execution import AgentExecutor, RequestContext
from a2a.server.events import EventQueue
from a2a.utils import new_agent_text_message

logger = logging.getLogger(__name__)

# Microsoft Agent Framework
from agent_framework import ChatAgent
from agent_framework.azure import AzureOpenAIChatClient
from azure.identity import AzureCliCredential

from services.financial_service import FinancialService
from tools.financial_tools import FinancialTools
from tools import financial_processing_tools


class FinancialAnalysisAgentExecutor(AgentExecutor):
    """
    AgentExecutor for financial analysis operations using Microsoft Agent Framework.
    """

    def __init__(self):
        """Initialize the financial analysis agent executor."""
        # Initialize financial services and tools
        self.financial_service = FinancialService()
        self.financial_tools = FinancialTools(self.financial_service)
        
        # Initialize the financial analysis agent
        self.agent = self._create_financial_agent()

    def _create_financial_agent(self) -> ChatAgent:
        """Create and configure the ChatAgent with financial analysis capabilities."""
        try:
            agent = ChatAgent(
                chat_client=AzureOpenAIChatClient(credential=AzureCliCredential()),
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
                    self.financial_tools.search_sales_data,
                    self.financial_tools.analyze_revenue_trends,
                    self.financial_tools.calculate_business_metrics,
                    self.financial_tools.get_top_performing_products,
                    self.financial_tools.analyze_customer_metrics,
                    self.financial_tools.get_financial_summary,
                    
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

    @override
    async def execute(
        self,
        context: RequestContext,
        event_queue: EventQueue,
    ) -> None:
        """
        Execute the financial analysis and return a message.
        
        Args:
            context: The request context containing user input
            event_queue: Event queue for sending message responses
        """
        logger.info("Execute method called")
        logger.info(f"Context: {context}")
        logger.info(f"Context message: {context.message if hasattr(context, 'message') else 'No message attr'}")
        
        query = context.get_user_input()
        logger.info(f"User query: {query}")

        if not context.message:
            logger.error('No message provided in context')
            raise Exception('No message provided')

        try:
            logger.info("Starting execution")
            # Handle empty queries with introduction
            if not query or query.strip() == "":
                logger.info("Empty query - sending introduction")
                response_text = (
                    "Hi, I'm the Python Financial Analysis Agent! 🔢 "
                    "I can help you with sales data analysis, revenue trends, "
                    "business metrics calculation, customer analytics, and financial reporting. "
                    "What would you like to analyze today?"
                )
            elif self.agent:
                # Process the query using Microsoft Agent Framework
                logger.info(f"Processing query with agent: {query}")
                response_content = await self.agent.run(query)
                response_text = response_content.text
                logger.info(f"Agent response: {response_text[:100]}...")
            else:
                # Fallback response if agent framework isn't available
                logger.warning("Agent framework not available")
                response_text = (
                    f"I received your question: '{query}'. "
                    "I'm a financial analysis agent ready to help with business metrics, "
                    "sales data, and financial reporting. However, the agent framework "
                    "is currently unavailable. Please check your Azure configuration."
                )

            # Send the response as a message
            message = new_agent_text_message(response_text)
            await event_queue.enqueue_event(message)
            logger.info("Message sent successfully")

        except Exception as e:
            # Handle errors gracefully
            logger.error(f"Error during execution: {e}", exc_info=True)
            error_message = f"An error occurred while processing your request: {str(e)}"
            
            # Send error as a message
            message = new_agent_text_message(error_message)
            await event_queue.enqueue_event(message)
            logger.error("Error message sent")

    @override
    async def cancel(
        self, context: RequestContext, event_queue: EventQueue
    ) -> None:
        """
        Cancel the current operation.
        
        Args:
            context: The request context
            event_queue: Event queue for sending updates
        """
        logger.info("Cancel method called")
        # Send cancellation message
        message = new_agent_text_message("Operation cancelled by user")
        await event_queue.enqueue_event(message)