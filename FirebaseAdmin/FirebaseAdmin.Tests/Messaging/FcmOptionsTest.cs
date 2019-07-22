using System;
using FirebaseAdmin.Messaging;
using FirebaseAdmin.Messaging.Util;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class FcmOptionsTest
    {
        [Fact]
        public void FcmOptionsCopyAndValidate()
        {
            var options = new FcmOptions() { AnalyticsLabel = "label" };
            var result = options.CopyAndValidate();
            Assert.Equal(options.AnalyticsLabel, result.AnalyticsLabel);
        }

        [Fact]
        public void ApnsFcmOptionsCopyAndValidate()
        {
            var options = new ApnsFcmOptions() { AnalyticsLabel = "label" };
            var result = options.CopyAndValidate();
            Assert.Equal(options.AnalyticsLabel, result.AnalyticsLabel);
        }

        [Fact]
        public void AndroidFcmOptionsCopyAndValidate()
        {
            var options = new AndroidFcmOptions() { AnalyticsLabel = "label" };
            var result = options.CopyAndValidate();
            Assert.Equal(options.AnalyticsLabel, result.AnalyticsLabel);
        }

        [Fact]
        public void FcmOptionsCopyAndValidateNullLabel()
        {
            var options = new FcmOptions() { AnalyticsLabel = null };
            Assert.Throws<ArgumentException>(() => options.CopyAndValidate());
        }

        [Fact]
        public void AnalyticsLabelTooLong()
        {
            Assert.Throws<ArgumentException>(() => AnalyticsLabelChecker.ValidateAnalyticsLabel("012345678901234567890123456789012345678901234567890"));
        }

        [Fact]
        public void AnalyticsLabelEmtpty()
        {
            Assert.Throws<ArgumentException>(() => AnalyticsLabelChecker.ValidateAnalyticsLabel(string.Empty));
        }

        [Fact]
        public void AnalyticsLabelInvalidCharacters()
        {
            Assert.Throws<ArgumentException>(() => AnalyticsLabelChecker.ValidateAnalyticsLabel("label(label)"));
        }
    }
}