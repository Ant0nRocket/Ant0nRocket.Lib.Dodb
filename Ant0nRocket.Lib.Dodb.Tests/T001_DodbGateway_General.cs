﻿using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Enums;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads.Mock;
using Ant0nRocket.Lib.Dodb.Tests.Extensions;

using NUnit.Framework;

using System;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    public class T001_DodbGateway_General
    {
        [Test]
        public void T001_SendingUnHandledDtoType()
        {
            var dto = new DtoOf<NotHandledPayload>() { UserId = Guid.NewGuid() };
            var result = Dodb.PushDto(dto);

            result
                .AssertIs<GrDtoPushFailed>()
                .AssertFailReasonIs(GrPushFailReason.PayloadHandlerNotFound);
        }

        [Test]
        public void T003_1_SendingDtoWithAnnotationValidationErrors()
        {
            var dto = new DtoOf<AnnotatedPayload>
            {
                Payload = new()
                {
                    SomeIntValue = -10,
                    SomeStringValue = "Hello world"
                },
                UserId = Guid.NewGuid(), // mock, for passing basic validation
            };

            var result = Dodb.PushDto(dto);

            result
                .AssertIs<GrDtoPushFailed>()
                .AssertFailReasonIs(GrPushFailReason.ValidationFailed);
        }

        [Test]
        public void T003_2_SendingDtoWithIValidatableImplementation()
        {
            var dto = new DtoOf<ValidatablePayload>
            {
                Payload = new() { TestValue = 11 },
                UserId = Guid.NewGuid(), // mock, for passing basic validation
                DateCreatedUtc = DateTime.Now // same reason
            };

            var result = Dodb.PushDto(dto);

            result
                .AssertIs<GrDtoPushFailed>()
                .AssertFailReasonIs(GrPushFailReason.ValidationFailed);
        }

        [Test]
        public void T007_ValidationOfLists()
        {
            var dto = Dodb.CreateDto<ListPayload>();
            var pushResult = Dodb.PushDto(dto);
            pushResult
                .AssertIs<GrDtoPushFailed>()
                .AssertFailReasonIs(GrPushFailReason.ValidationFailed); // no items added

            // Valid
            dto.Payload.Items.Add(new() { SomeIntValue = 10, SomeStringValue = "12" });
            pushResult = Dodb.PushDto(dto);
            Assert.That(pushResult is GrDtoPushSuccess);

            // Invalid
            dto.Payload.Items.Clear();
            dto.Payload.Items.Add(new() { SomeIntValue = -10, SomeStringValue = "12" }); // -10 is invalid
            pushResult = Dodb.PushDto(dto);
            pushResult
                .AssertIs<GrDtoPushFailed>()
                .AssertFailReasonIs(GrPushFailReason.ValidationFailed);

            // Invalid + valid = invalid
            dto.Payload.Items.Clear();
            dto.Payload.Items.Add(new() { SomeIntValue = 10, SomeStringValue = "12" }); // valid
            dto.Payload.Items.Add(new() { SomeIntValue = -10, SomeStringValue = "12" }); // valid
            pushResult = Dodb.PushDto(dto);
            pushResult
                .AssertIs<GrDtoPushFailed>()
                .AssertFailReasonIs(GrPushFailReason.ValidationFailed);
        }

        
    }
}
