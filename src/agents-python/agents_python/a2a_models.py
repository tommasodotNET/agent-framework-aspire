"""
A2A (Agent-to-Agent) protocol models for inter-agent communication.
These models match the Microsoft Agent Framework A2A protocol specification.
"""
from typing import List, Optional, Dict, Any
from pydantic import BaseModel, Field, ConfigDict
from enum import Enum


def to_camel_case(snake_str: str) -> str:
    """Convert snake_case to camelCase."""
    components = snake_str.split('_')
    return components[0] + ''.join(word.capitalize() for word in components[1:])


class AgentSkill(BaseModel):
    """Represents a skill or capability that the agent can perform."""
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    id: str
    name: str
    description: str
    examples: List[str]
    tags: List[str] = Field(default_factory=list)


class AgentCapabilities(BaseModel):
    """Represents the capabilities of an agent."""
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    streaming: bool
    push_notifications: bool


class AgentCard(BaseModel):
    """
    Represents an agent card containing metadata and capabilities.
    This is returned by the /agenta2a/v1/card endpoint.
    """
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    name: str
    url: str
    description: str
    version: str
    protocol_version: str = "1.0"
    default_input_modes: List[str]
    default_output_modes: List[str]
    capabilities: AgentCapabilities
    skills: List[AgentSkill]


# JSON-RPC 2.0 Models for A2A Communication

class MessagePart(BaseModel):
    """A part of a message representing TextPart from .NET."""
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    kind: str = "text"
    text: str
    metadata: Optional[Dict[str, Any]] = None


class AgentMessageResponse(BaseModel):
    """AgentMessage response structure matching .NET AgentMessage class."""
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    kind: str = "message"
    role: str = "agent"
    parts: List[MessagePart]
    metadata: Optional[Dict[str, Any]] = None
    reference_task_ids: Optional[List[str]] = None
    message_id: str
    task_id: Optional[str] = None
    context_id: Optional[str] = None
    extensions: Optional[List[str]] = None


class JsonRpcMessage(BaseModel):
    """JSON-RPC message structure."""
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    kind: str
    role: str
    parts: List[MessagePart]
    message_id: Optional[str] = None


class JsonRpcParams(BaseModel):
    """JSON-RPC parameters."""
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    message: JsonRpcMessage


class JsonRpcRequest(BaseModel):
    """JSON-RPC 2.0 request structure."""
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    jsonrpc: str
    id: str
    method: str
    params: JsonRpcParams


class JsonRpcResponse(BaseModel):
    """JSON-RPC 2.0 response structure."""
    model_config = ConfigDict(
        alias_generator=to_camel_case,
        populate_by_name=True
    )
    
    jsonrpc: str = "2.0"
    id: str
    result: AgentMessageResponse