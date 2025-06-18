using System;
using System.Collections.Generic;
using Shouldly;
using Volo.Abp.Internal.Telemetry.Activity;
using Xunit;

namespace Volo.Abp.Telemetry;

public class ActivityEvent_Tests 
{
    [Fact]
    public void Should_Create_ActivityEvent_With_Required_Parameters()
    {
        // Arrange
        var activityName = "TestActivity";
        var details = "Test Details";

        // Act
        var activityEvent = new ActivityEvent(activityName, details);

        // Assert
        activityEvent.ActivityName.ShouldBe(activityName);
        activityEvent.ActivityDetails.ShouldBe(details);
        activityEvent.Id.ShouldNotBe(Guid.Empty);
        activityEvent.Time.ShouldNotBe(default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Throw_Exception_When_ActivityName_Is_Invalid(string invalidName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new ActivityEvent(invalidName));
    }

    [Fact]
    public void Should_Set_And_Get_AdditionalProperties()
    {
        // Arrange
        var activityEvent = new ActivityEvent("TestActivity");
        var additionalProps = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };

        // Act
        activityEvent.AdditionalProperties = additionalProps;

        // Assert
        activityEvent.AdditionalProperties.ShouldNotBeNull();
        activityEvent.AdditionalProperties.Count.ShouldBe(2);
        activityEvent.AdditionalProperties["key1"].ShouldBe("value1");
        activityEvent.AdditionalProperties["key2"].ShouldBe(42);
    }

    [Fact]
    public void Should_Return_Default_Values_When_Properties_Not_Set()
    {
        // Arrange
        var activityEvent = new ActivityEvent("TestActivity");

        // Assert
        activityEvent.ActivityDetails.ShouldBeNull();
        activityEvent.ActivityDuration.ShouldBeNull();
        activityEvent.AdditionalProperties.ShouldBeNull();
    }

    [Fact]
    public void Should_Behave_Like_Dictionary()
    {
        // Arrange
        var activityEvent = new ActivityEvent("TestActivity");

        // Act
        activityEvent["CustomKey"] = "CustomValue";

        // Assert
        activityEvent["CustomKey"].ShouldBe("CustomValue");
        activityEvent.ContainsKey("CustomKey").ShouldBeTrue();
    }


}

