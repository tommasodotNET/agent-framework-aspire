"""
A2A (Agent-to-Agent) protocol models for Python agents.
Based on the Microsoft Agent Framework A2A protocol specification.
"""
from typing import List, Optional, Dict, Any
from pydantic import BaseModel, Field
from enum import Enum


class AgentCapabilities(BaseModel):
    """Agent capabilities configuration."""
    streaming: bool = Field(default=True, description="Whether the agent supports streaming responses")
    push_notifications: bool = Field(default=False, alias="pushNotifications", description="Whether the agent supports push notifications")


class AgentSkill(BaseModel):
    """Represents a skill that an agent can perform."""
    id: str = Field(description="Unique identifier for the skill")
    name: str = Field(description="Name of the skill")
    description: str = Field(description="Description of what the skill does")
    examples: List[str] = Field(default_factory=list, description="Example queries that demonstrate this skill")
    tags: List[str] = Field(default_factory=list, description="Tags categorizing the skill")


class AgentCard(BaseModel):
    """Agent card containing metadata about the agent."""
    name: str = Field(description="Agent name")
    description: str = Field(description="Agent description")
    url: str = Field(description="Agent endpoint URL")
    version: str = Field(description="Agent version")
    protocol_version: str = Field(default="1.0", alias="protocolVersion", description="A2A protocol version")
    default_input_modes: List[str] = Field(default_factory=lambda: ["text"], alias="defaultInputModes")
    default_output_modes: List[str] = Field(default_factory=lambda: ["text"], alias="defaultOutputModes")
    capabilities: AgentCapabilities = Field(default_factory=AgentCapabilities)
    skills: List[AgentSkill] = Field(default_factory=list)


class A2ATaskStatus(str, Enum):
    """A2A task status enumeration."""
    pending = "pending"
    running = "running"
    completed = "completed"
    failed = "failed"
    cancelled = "cancelled"


class A2AMessage(BaseModel):
    """A2A protocol message."""
    id: str = Field(description="Message ID")
    type: str = Field(description="Message type")
    content: Any = Field(description="Message content")
    timestamp: Optional[str] = None
    sender: Optional[str] = None
    recipient: Optional[str] = None


class A2ATaskRequest(BaseModel):
    """A2A task execution request."""
    id: str = Field(description="Task ID")
    agent_id: str = Field(alias="agentId", description="Target agent ID")
    task_type: str = Field(alias="taskType", description="Type of task to execute")
    input_data: Dict[str, Any] = Field(alias="inputData", description="Input data for the task")
    parameters: Optional[Dict[str, Any]] = None
    metadata: Optional[Dict[str, Any]] = None


class A2ATaskResponse(BaseModel):
    """A2A task execution response."""
    id: str = Field(description="Task ID")
    status: A2ATaskStatus = Field(description="Task status")
    result: Optional[Any] = None
    error: Optional[str] = None
    metadata: Optional[Dict[str, Any]] = None


class A2AAgentDiscoveryRequest(BaseModel):
    """A2A agent discovery request."""
    requester_id: str = Field(alias="requesterId", description="ID of the requesting agent")
    capabilities_filter: Optional[List[str]] = Field(default=None, alias="capabilitiesFilter")


class A2AAgentDiscoveryResponse(BaseModel):
    """A2A agent discovery response."""
    agents: List[AgentCard] = Field(description="List of available agents")