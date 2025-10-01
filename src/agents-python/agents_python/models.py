from typing import List, Optional
from pydantic import BaseModel, Field
from enum import Enum


class AIChatRole(str, Enum):
    """Chat role enumeration matching the .NET AIChatRole enum."""
    system = "system"
    assistant = "assistant"
    user = "user"


class AIChatFile(BaseModel):
    """Chat file attachment matching the .NET AIChatFile struct."""
    content_type: str = Field(alias="contentType")
    data: str  # Base64 encoded data


class AIChatMessage(BaseModel):
    """Chat message matching the .NET AIChatMessage struct."""
    content: str
    role: AIChatRole
    context: Optional[str] = None
    files: Optional[List[AIChatFile]] = None


class AIChatRequest(BaseModel):
    """Chat request matching the .NET AIChatRequest record."""
    messages: List[AIChatMessage]
    session_state: Optional[str] = Field(default=None, alias="sessionState")
    context: Optional[str] = None


class AIChatMessageDelta(BaseModel):
    """Chat message delta matching the .NET AIChatMessageDelta struct."""
    content: Optional[str] = None
    role: Optional[AIChatRole] = None
    context: Optional[str] = None


class AIChatCompletionDelta(BaseModel):
    """Chat completion delta matching the .NET AIChatCompletionDelta record."""
    delta: AIChatMessageDelta
    session_state: Optional[str] = Field(default=None, alias="sessionState")
    context: Optional[str] = None

    def __init__(self, delta: AIChatMessageDelta, **data):
        super().__init__(delta=delta, **data)

    class Config:
        populate_by_name = True  # Allow both snake_case and camelCase