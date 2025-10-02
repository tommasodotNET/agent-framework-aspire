"""
A2A Host Agent implementation for Python agents.
Provides A2A protocol support for wrapping existing ChatAgent instances.
"""
import uuid
import asyncio
from typing import Dict, Any, Optional
from datetime import datetime
import json

from agent_framework import ChatAgent
from .a2a_models import (
    AgentCard, AgentSkill, AgentCapabilities, A2ATaskRequest, 
    A2ATaskResponse, A2ATaskStatus, A2AMessage
)
from .models import AIChatMessage, AIChatRole


class A2ATaskManager:
    """Manages A2A tasks for an agent."""
    
    def __init__(self, agent: ChatAgent, agent_card: AgentCard, execution_agent: ChatAgent = None):
        self.agent = agent  # Simple agent for A2A protocol
        self.execution_agent = execution_agent or agent  # Full-featured agent for actual execution
        self.agent_card = agent_card
        self.running_tasks: Dict[str, asyncio.Task] = {}
        self.task_results: Dict[str, A2ATaskResponse] = {}
    
    async def start_task(self, request: A2ATaskRequest) -> A2ATaskResponse:
        """Start executing an A2A task."""
        try:
            # Create initial response
            response = A2ATaskResponse(
                id=request.id,
                status=A2ATaskStatus.pending,
                metadata={"started_at": datetime.utcnow().isoformat()}
            )
            
            # Store the response
            self.task_results[request.id] = response
            
            # Start the task execution
            task = asyncio.create_task(self._execute_task(request))
            self.running_tasks[request.id] = task
            
            # Update status to running
            response.status = A2ATaskStatus.running
            self.task_results[request.id] = response
            
            return response
            
        except Exception as e:
            error_response = A2ATaskResponse(
                id=request.id,
                status=A2ATaskStatus.failed,
                error=str(e)
            )
            self.task_results[request.id] = error_response
            return error_response
    
    async def _execute_task(self, request: A2ATaskRequest):
        """Execute the actual task."""
        try:
            # Extract the user message from input data
            user_input = request.input_data.get("message", "")
            if isinstance(request.input_data.get("messages"), list):
                # Handle messages array format
                messages = request.input_data["messages"]
                if messages:
                    user_input = messages[-1].get("content", "")
            
            # Convert to ChatAgent format
            chat_message = AIChatMessage(
                content=user_input,
                role=AIChatRole.user
            )
            
            # Execute with the execution agent (which has all the tools)
            result = await self.execution_agent.run(user_input)
            
            # Update task status to completed
            response = A2ATaskResponse(
                id=request.id,
                status=A2ATaskStatus.completed,
                result={
                    "text": result.text if hasattr(result, 'text') else str(result),
                    "type": "text"
                },
                metadata={
                    "completed_at": datetime.utcnow().isoformat()
                }
            )
            
            self.task_results[request.id] = response
            
            # Clean up
            if request.id in self.running_tasks:
                del self.running_tasks[request.id]
                
        except Exception as e:
            # Update task status to failed
            response = A2ATaskResponse(
                id=request.id,
                status=A2ATaskStatus.failed,
                error=str(e),
                metadata={
                    "failed_at": datetime.utcnow().isoformat()
                }
            )
            self.task_results[request.id] = response
            
            # Clean up
            if request.id in self.running_tasks:
                del self.running_tasks[request.id]
    
    async def get_task_status(self, task_id: str) -> Optional[A2ATaskResponse]:
        """Get the status of a specific task."""
        return self.task_results.get(task_id)
    
    async def cancel_task(self, task_id: str) -> bool:
        """Cancel a running task."""
        if task_id in self.running_tasks:
            task = self.running_tasks[task_id]
            task.cancel()
            
            # Update status
            response = A2ATaskResponse(
                id=task_id,
                status=A2ATaskStatus.cancelled,
                metadata={
                    "cancelled_at": datetime.utcnow().isoformat()
                }
            )
            self.task_results[task_id] = response
            
            # Clean up
            del self.running_tasks[task_id]
            return True
        
        return False


class A2AHostAgent:
    """A2A Host Agent that wraps a ChatAgent to provide A2A protocol support."""
    
    def __init__(self, agent: ChatAgent, agent_card: AgentCard, execution_agent: ChatAgent = None):
        self.agent = agent  # Simple agent for A2A protocol
        self.agent_card = agent_card
        self.task_manager = A2ATaskManager(agent, agent_card, execution_agent)
        self.agent_id = str(uuid.uuid4())
    
    def get_agent_card(self) -> AgentCard:
        """Get the agent card for discovery."""
        return self.agent_card
    
    async def handle_discovery_request(self) -> Dict[str, Any]:
        """Handle agent discovery requests."""
        # Return the agent card directly in the expected A2A format
        # We create a clean response without exposing the underlying ChatAgent tools
        return {
            "name": self.agent_card.name,
            "description": self.agent_card.description,
            "url": self.agent_card.url,
            "version": self.agent_card.version,
            "protocolVersion": self.agent_card.protocol_version,
            "defaultInputModes": self.agent_card.default_input_modes,
            "defaultOutputModes": self.agent_card.default_output_modes,
            "capabilities": {
                "streaming": self.agent_card.capabilities.streaming,
                "pushNotifications": self.agent_card.capabilities.push_notifications
            },
            "skills": [
                {
                    "id": skill.id,
                    "name": skill.name,
                    "description": skill.description,
                    "examples": skill.examples,
                    "tags": skill.tags
                }
                for skill in self.agent_card.skills
            ]
        }
    
    async def handle_task_request(self, request_data: Dict[str, Any]) -> Dict[str, Any]:
        """Handle A2A task execution requests."""
        try:
            # Parse the request
            task_request = A2ATaskRequest(**request_data)
            
            # Start the task
            response = await self.task_manager.start_task(task_request)
            
            return response.model_dump(by_alias=True)
            
        except Exception as e:
            return {
                "id": request_data.get("id", str(uuid.uuid4())),
                "status": "failed",
                "error": str(e)
            }
    
    async def handle_task_status_request(self, task_id: str) -> Dict[str, Any]:
        """Handle task status requests."""
        response = await self.task_manager.get_task_status(task_id)
        if response:
            return response.model_dump(by_alias=True)
        else:
            return {
                "id": task_id,
                "status": "not_found",
                "error": "Task not found"
            }
    
    async def handle_task_cancellation(self, task_id: str) -> Dict[str, Any]:
        """Handle task cancellation requests."""
        success = await self.task_manager.cancel_task(task_id)
        return {
            "id": task_id,
            "cancelled": success
        }