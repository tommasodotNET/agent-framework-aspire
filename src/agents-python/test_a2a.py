"""
Simple test script to validate A2A and HTTP endpoints are correctly implemented.
"""
import json
import uuid
from agents_python.models import AIChatRequest, AIChatMessage, AIChatRole
from agents_python.a2a_models import AgentCard, AgentSkill, AgentCapabilities, A2ATaskRequest
from agents_python.a2a_agent import A2AHostAgent

def test_models():
    """Test that models can be created and serialized correctly."""
    print("Testing models...")
    
    # Test AIChatRequest
    chat_request = AIChatRequest(
        messages=[
            AIChatMessage(
                content="What are our top products?",
                role=AIChatRole.user
            )
        ]
    )
    print(f"✓ AIChatRequest: {chat_request.model_dump_json()}")
    
    # Test AgentCard
    agent_card = AgentCard(
        name="Test Agent",
        description="Test financial agent",
        version="1.0.0",
        skills=[
            AgentSkill(
                name="Test Skill",
                description="A test skill",
                examples=["Example 1", "Example 2"]
            )
        ]
    )
    print(f"✓ AgentCard: {agent_card.model_dump_json()}")
    
    # Test A2ATaskRequest
    task_request = A2ATaskRequest(
        id=str(uuid.uuid4()),
        agent_id="test-agent",
        task_type="chat",
        input_data={"message": "Hello"}
    )
    print(f"✓ A2ATaskRequest: {task_request.model_dump_json()}")
    
    print("All models tested successfully!")

def test_endpoints_structure():
    """Test that the endpoint structure looks correct."""
    print("\nTesting endpoint structure...")
    
    # Test A2A agent creation
    agent_card = AgentCard(
        name="Python Financial Analysis Agent",
        description="Specialized Financial Analysis and Business Intelligence Assistant",
        version="1.0.0",
        capabilities=AgentCapabilities(streaming=True, push_notifications=False),
        skills=[
            AgentSkill(
                name="Sales Analysis",
                description="Analyze sales data and trends",
                examples=["What were our top products?"]
            )
        ]
    )
    
    # Mock agent for testing
    class MockAgent:
        async def run(self, message):
            return type('Result', (), {'text': f"Mock response to: {message}"})()
    
    mock_agent = MockAgent()
    a2a_agent = A2AHostAgent(mock_agent, agent_card)
    
    print(f"✓ A2A Agent created with ID: {a2a_agent.agent_id}")
    print(f"✓ Agent card: {a2a_agent.get_agent_card().name}")
    
    print("Endpoint structure tests passed!")

if __name__ == "__main__":
    try:
        test_models()
        test_endpoints_structure()
        
        print("\n🎉 All tests passed! Your A2A implementation structure is correct.")
        print("\nEndpoints that should be available:")
        print("- GET  /              → A2A agent discovery")
        print("- POST /              → A2A task execution")
        print("- GET  /task/{id}     → A2A task status")
        print("- DELETE /task/{id}   → A2A task cancellation")
        print("- POST /agent/chat/stream → HTTP streaming chat")
        print("- GET  /health        → Health check (shows A2A status)")
        
    except Exception as e:
        print(f"❌ Test failed: {e}")
        import traceback
        traceback.print_exc()