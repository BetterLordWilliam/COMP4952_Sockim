# Chat Application - Implementation Quick Reference

## Current Status ✅

### ✅ Completed
- **Chat Creation Flow** - End-to-end working
- **Unified DTO Architecture** - All client-server communication uses DTOs
- **ChatHub** - Fully implemented with proper patterns
- **Services** - Refactored to work with DTOs
- **ChatCreateForm** - Refactored to use SignalR, no entity injection
- **Database** - Entities untouched, all working as expected

### 📋 Next Steps (Priority Order)

---

## 1. Update Hub Registrations in Program.cs

Your `Program.cs` currently has:
```csharp
app.MapHub<TestChatHub>("chathubtest");
app.MapHub<ChatHub>("chathub");
```

You'll want to add (when ready):
```csharp
// When implementing InvitationHub
// app.MapHub<InvitationHub>("invitationhub");

// When implementing MessageHub  
// app.MapHub<MessageHub>("messagehub");
```

---

## 2. Implement InvitationHub

**File:** `Hubs/InvitationHub.cs`

**Key Methods:**
```csharp
public async Task AcceptInvitation(int invitationSenderId, int chatId, int receiverId)
public async Task RejectInvitation(int invitationSenderId, int chatId, int receiverId)  
public async Task GetPendingInvitations(int userId)
```

**Broadcasting:**
```csharp
// When invitation accepted
await Clients.All.SendAsync("InvitationAccepted", new { ChatId = chatId, UserId = receiverId });

// When invitation rejected
await Clients.All.SendAsync("InvitationRejected", new { ChatId = chatId, UserId = receiverId });

// Send pending list to specific user
await Clients.Caller.SendAsync("PendingInvitations", invitationDtos);
```

See: `Hubs/InvitationHubTemplate.cs` for full template

---

## 3. Implement MessageHub

**File:** `Hubs/MessageHub.cs`

**Key Methods:**
```csharp
public async Task JoinChat(int chatId, int userId)
public async Task LeaveChat(int chatId, int userId)
public async Task SendMessage(int chatId, ChatMessageDto messageDto)
public async Task GetChatHistory(int chatId)
```

**Group-based Broadcasting:**
```csharp
// Join user to chat group
string groupName = $"chat-{chatId}";
await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

// Broadcast message to chat group
await Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);
```

See: `Hubs/MessageHubTemplate.cs` for full template

---

## 4. Create/Update Blade Components

### Create `ChatInvitationsList.razor`
Display pending invitations with Accept/Reject buttons
```razor
@foreach (ChatInvitationDto invitation in _invitations)
{
    <div>
        <span>@invitation.SenderEmail invited you to a chat</span>
        <button @onclick="() => AcceptInvitation(invitation)">Accept</button>
        <button @onclick="() => RejectInvitation(invitation)">Reject</button>
    </div>
}
```

### Update `ChatForm.razor` for MessageHub
Already partially done - just needs:
- Connect to MessageHub on chat open
- Call `JoinChat(chatId, userId)` 
- Call `GetChatHistory(chatId)`
- Handle "UserJoined"/"UserLeft" events (optional)

### Create `UsersList.razor` (Optional)
Show who's currently in the chat

---

## 5. Frontend Connection Pattern

### For InvitationHub
```csharp
private HubConnection? _invitationHub;

// In OnInitializedAsync
_invitationHub = new HubConnectionBuilder()
    .WithUrl(NavManager.ToAbsoluteUri("/invitationhub"))
    .WithAutomaticReconnect()
    .Build();

_invitationHub.On<ChatInvitationDto[]>("PendingInvitations", async (invitations) =>
{
    _pendingInvitations = invitations.ToList();
    await InvokeAsync(StateHasChanged);
});

await _invitationHub.StartAsync();

// Send invitation acceptance
await _invitationHub.SendAsync("AcceptInvitation", senderId, chatId, receiverId);
```

### For MessageHub
Already largely implemented in `ChatForm.razor` - just update:
```csharp
// In OnInitializedAsync
await _hubConnection.SendAsync("JoinChat", Chat.Id, _user.Id);

// When getting history
await _hubConnection.SendAsync("GetChatHistory", Chat.Id);

// Existing send is good, just rename group broadcast to be consistent
_hubConnection.On<ChatMessageDto>("ReceiveMessage", async (messageDto) =>
{
    _messages.Add(messageDto);
    await InvokeAsync(StateHasChanged);
});
```

---

## Data Flow Examples

### Accept Invitation Flow
```
User clicks "Accept" button
    ↓
InvitationHub.AcceptInvitation(senderId, chatId, receiverId)
    ↓
[Update DB: invitation.Accepted = true]
    ↓
[Add user to chat.ChatUsers]
    ↓
Broadcast: Clients.All.SendAsync("InvitationAccepted", ...)
    ↓
All clients: "InvitationAccepted" event received
    ↓
Update local invitations list (remove accepted)
Update chat members list (add new member)
```

### Message Send Flow (Already Working)
```
User types message and clicks Send
    ↓
ChatForm creates ChatMessageDto
    ↓
MessagesService.AddChatMessage(dto)  [saves to DB]
    ↓
Hub.SendMessage(chatId, messageDto)
    ↓
Broadcast: Clients.Group("chat-X").SendAsync("ReceiveMessage", dto)
    ↓
Connected clients: "ReceiveMessage" event
    ↓
Add messageDto to _messages list, update UI
```

---

## Service Method Reference

### ChatService
```csharp
// CREATE
await chatService.AddChatWithInvitations(ChatDto) → Task<ChatDto>

// READ
Chat? chatService.GetChatById(int) 
Chat[] chatService.GetChatsForUser(ChatUser)

// CONVERT
ChatDto chatService.ConvertToDto(Chat)
```

### InvitationsService
```csharp
// CREATE
await invService.AddInvitations(ChatInvitationDto[])
await invService.AddInvitation(ChatInvitationDto)

// CONVERT
ChatInvitationDto invService.ConvertToDto(ChatInvitation)
```

### MessagesService
```csharp
// CREATE
await messagesService.AddChatMessage(ChatMessageDto) → Task<ChatMessageDto>

// READ
ChatMessageDto[] messagesService.GetChatMessages(int chatId)

// CONVERT
ChatMessageDto messagesService.ConvertToDto(ChatMessage)
```

### ChatUserService (Unchanged)
```csharp
// READ
ChatUser? chatUserService.GetUser(int? id)
ChatUser? chatUserService.GetUserByEmail(string)
ChatUser[]? chatUserService.GetUserByEmail(string[])
```

---

## Common Patterns

### DTO to Entity Conversion (in Services)
```csharp
public async Task AddInvitations(ChatInvitationDto[] invitationDtos)
{
    List<ChatInvitation> invitations = [];
    foreach (var dto in invitationDtos)
    {
        invitations.Add(new ChatInvitation
        {
            SenderId = dto.SenderId,
            ReceiverId = dto.ReceiverId,
            ChatId = dto.ChatId,
            Accepted = dto.Accepted
        });
    }
    
    await _chatDbContext.Invitations.AddRangeAsync(invitations);
    await _chatDbContext.SaveChangesAsync();
}
```

### Entity to DTO Conversion (in Services)
```csharp
public ChatDto ConvertToDto(Chat chat)
{
    return new ChatDto
    {
        Id = chat.Id,
        ChatName = chat.ChatName,
        ChatOwnerId = chat.ChatOwnerId,
        ChatOwnerEmail = chat.ChatOwner?.Email ?? string.Empty,
        InvitedEmails = chat.ChatUsers
            .Where(u => u.Id != chat.ChatOwnerId)
            .Select(u => u.Email ?? string.Empty)
            .ToList()
    };
}
```

### Hub Error Handling Pattern
```csharp
public async Task SomeHubMethod(SomeDto dto)
{
    try
    {
        // Do work
        await Clients.Caller.SendAsync("Success", result);
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error in SomeHubMethod: {ex.Message}");
        await Clients.Caller.SendAsync("Error", new 
        { 
            message = "Operation failed", 
            error = ex.Message 
        });
    }
}
```

---

## Testing Checklist

- [ ] Chat creation with invitations
- [ ] Invitation acceptance flow
- [ ] Invitation rejection flow  
- [ ] Message sending within chat
- [ ] Message receiving in real-time
- [ ] Multiple users in same chat
- [ ] Connection loss and reconnect
- [ ] User typing indicators (if implemented)
- [ ] Chat history loading

---

## Troubleshooting

### Hub Connection Issues
1. Check hub is registered in Program.cs
2. Verify hub URL matches registration (`/chathub`, `/messagehub`, etc.)
3. Check browser console for connection errors
4. Verify authentication if needed

### DTO Serialization Issues
1. Ensure all DTO properties are serializable
2. Check SignalR JSON protocol in Program.cs has proper settings
3. Verify no circular references in DTOs

### Message Not Broadcasting
1. Check group name consistency (`$"chat-{chatId}"`)
2. Verify user joined group with `JoinChat()`
3. Check hub method parameters match client call

### Database Changes Not Showing
1. Verify SaveChangesAsync() is called
2. Check entity tracking isn't disabled (AsNoTracking)
3. Ensure transaction is committed

---

## Architecture Principles

🎯 **Remember:**
1. **DTOs cross hub boundaries** - never entities
2. **Services transform** - DTOs ↔ Entities
3. **Hubs orchestrate** - don't do business logic
4. **Groups for broadcasts** - for chat rooms, use groups
5. **Errors go to client** - always handle and send error events

---

Good luck! The boilerplate is now in place to build out the rest! 🚀
