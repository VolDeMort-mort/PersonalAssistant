using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public record SessionMessageDto(DtoMessageType Type, string? Text, string? FileId, DateTime CreatedAt, int MessageId);

