"""
A2A (Agent-to-Agent) protocol models for inter-agent communication.
These models match the Microsoft Agent Framework A2A protocol specification.
"""
from typing import List, Optional, Dict, Any
from pydantic import BaseModel, Field
from enum import Enum


class AgentSkill(BaseModel):
    """Represents a skill or capability that the agent can perform."""
    id: str = Field(alias="Id")
    name: str = Field(alias="Name")
    description: str = Field(alias="Description")
    examples: List[str] = Field(alias="Examples")
    tags: List[str] = Field(alias="Tags", default_factory=list)

    class Config:
        populate_by_name = True


class AgentCapabilities(BaseModel):
    """Represents the capabilities of an agent."""
    streaming: bool = Field(alias="Streaming")
    push_notifications: bool = Field(alias="PushNotifications")

    class Config:
        populate_by_name = True


class AgentCard(BaseModel):
    """
    Represents an agent card containing metadata and capabilities.
    This is returned by the /agenta2a/v1/card endpoint.
    """
    name: str = Field(alias="Name")
    url: str = Field(alias="Url")
    description: str = Field(alias="Description")
    version: str = Field(alias="Version")
    protocol_version: str = Field(alias="ProtocolVersion", default="1.0")
    default_input_modes: List[str] = Field(alias="DefaultInputModes")
    default_output_modes: List[str] = Field(alias="DefaultOutputModes")
    capabilities: AgentCapabilities = Field(alias="Capabilities")
    skills: List[AgentSkill] = Field(alias="Skills")

    class Config:
        populate_by_name = True


# JSON-RPC 2.0 Models for A2A Communication

class MessagePart(BaseModel):
    """A part of a message representing TextPart from .NET."""
    kind: str = Field(alias="kind", default="text")
    text: str = Field(alias="text")
    metadata: Optional[Dict[str, Any]] = Field(default=None, alias="metadata")

    class Config:
        populate_by_name = True


class AgentMessageResponse(BaseModel):
    """AgentMessage response structure matching .NET AgentMessage class."""
    kind: str = Field(alias="kind", default="message")
    role: str = Field(alias="role", default="agent")
    parts: List[MessagePart] = Field(alias="parts")
    metadata: Optional[Dict[str, Any]] = Field(default=None, alias="metadata")
    reference_task_ids: Optional[List[str]] = Field(default=None, alias="referenceTaskIds")
    message_id: str = Field(alias="messageId")
    task_id: Optional[str] = Field(default=None, alias="taskId")
    context_id: Optional[str] = Field(default=None, alias="contextId")
    extensions: Optional[List[str]] = Field(default=None, alias="extensions")

    class Config:
        populate_by_name = True


class JsonRpcMessage(BaseModel):
    """JSON-RPC message structure."""
    kind: str = Field(alias="kind")
    role: str = Field(alias="role")
    parts: List[MessagePart] = Field(alias="parts")
    message_id: Optional[str] = Field(default=None, alias="messageId")

    class Config:
        populate_by_name = True


class JsonRpcParams(BaseModel):
    """JSON-RPC parameters."""
    message: JsonRpcMessage = Field(alias="message")

    class Config:
        populate_by_name = True


class JsonRpcRequest(BaseModel):
    """JSON-RPC 2.0 request structure."""
    jsonrpc: str = Field(alias="jsonrpc")
    id: str = Field(alias="id")
    method: str = Field(alias="method")
    params: JsonRpcParams = Field(alias="params")

    class Config:
        populate_by_name = True


class JsonRpcResponse(BaseModel):
    """JSON-RPC 2.0 response structure."""
    jsonrpc: str = Field(alias="jsonrpc", default="2.0")
    id: str = Field(alias="id")
    result: AgentMessageResponse = Field(alias="result")

    class Config:
        populate_by_name = True