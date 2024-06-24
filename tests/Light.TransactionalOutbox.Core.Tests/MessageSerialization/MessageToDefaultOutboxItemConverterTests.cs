using System;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Light.GuardClauses;
using Light.TransactionalOutbox.Core.MessageSerialization;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Light.TransactionalOutbox.Core.Tests.MessageSerialization;

public sealed class MessageToDefaultOutboxItemConverterTests
{
    private readonly MessageToDefaultOutboxItemConverter _converter;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly FakeTimeProvider _timeProvider;

    public MessageToDefaultOutboxItemConverterTests()
    {
        var messageTypes = MessageTypes.CreateDefault();
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.TypeInfoResolverChain.Insert(0, MessagesJsonSerializerContext.Default);
        
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _converter = new MessageToDefaultOutboxItemConverter(
            messageTypes,
            _timeProvider,
            _jsonOptions
        );
    }

    [Fact]
    public void WhenNullIsPassed_ThrowArgumentNullException()
    {
        var act = () => _converter.Convert(null!);

        act.Should().Throw<ArgumentNullException>()
           .And.ParamName.Should().Be("message");
    }

    [Fact]
    public void WhenMessageWithoutCorrelationIdIsPassed_CreateANewOneForTheOutboxItem()
    {
        var message = new MyMessage(Guid.NewGuid(), "Foo");

        var outboxItem = _converter.Convert(message);

        var expectedOutboxItem = new DefaultOutboxItem
        {
            MessageType = typeof(MyMessage).GetCustomAttribute<MessageTypeAttribute>()!.PrimaryName,
            CorrelationId = outboxItem.CorrelationId.MustNotBe(Guid.Empty),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            SerializedMessage = JsonSerializer.Serialize(message, _jsonOptions)
        };
        outboxItem.Should().BeEquivalentTo(
            expectedOutboxItem,
            options => options.ComparingByMembers<DefaultOutboxItem>()
        );
    }

    [Fact]
    public void WhenMessageWithCorrelationIdIsPassed_ReuseItForTheOutboxItem()
    {
        var message = new MySecondMessage(Guid.NewGuid(), "Bar");

        var outboxItem = _converter.Convert(message);
        
        var expectedOutboxItem = new DefaultOutboxItem
        {
            MessageType = typeof(MySecondMessage).GetCustomAttribute<MessageTypeAttribute>()!.PrimaryName,
            CorrelationId = message.CorrelationId,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            SerializedMessage = JsonSerializer.Serialize(message, _jsonOptions)
        };
        outboxItem.Should().BeEquivalentTo(
            expectedOutboxItem,
            options => options.ComparingByMembers<DefaultOutboxItem>()
        );
    }

    [Fact]
    public void WhenAnUnknownMessageIsPassed_ThrowAnInvalidOperationException()
    {
        var unknownMessage = new UnknownMessage(Guid.NewGuid(), "Baz");
        
        var act = () => _converter.Convert(unknownMessage);

        act.Should().Throw<InvalidOperationException>().And
           .Message.Should().Be(
                $"No message type found for type \"{typeof(UnknownMessage)}\". Please ensure to register all message types with the {nameof(MessageTypes)} class."
            );
    }

    // ReSharper disable NotAccessedPositionalProperty.Local -- the properties are used for serialization purposes
    private sealed record UnknownMessage(Guid Id, string Content);
    // ReSharper restore NotAccessedPositionalProperty.Local
}