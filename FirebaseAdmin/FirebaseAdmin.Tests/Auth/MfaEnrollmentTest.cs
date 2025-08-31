using System;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Users;
using Xunit;

namespace FirebaseAdmin.Tests.Auth
{
    public class MfaEnrollmentTest
    {
        [Fact]
        public void NullResponse()
        {
            Assert.Throws<ArgumentNullException>(() => new MfaEnrollment(null));
        }

        [Fact]
        public void EmptyUid()
        {
            Assert.Throws<ArgumentException>(() => new MfaEnrollment(new GetAccountInfoResponse.MfaEnrollment()
            {
                MfaEnrollmentId = string.Empty,
            }));
        }

        [Fact]
        public void NoInfo()
        {
            Assert.Throws<ArgumentException>(() => new MfaEnrollment(new GetAccountInfoResponse.MfaEnrollment()
            {
                MfaEnrollmentId = "testId",
                PhoneInfo = null,
                TotpInfo = null,
            }));
        }

        [Fact]
        public void ConflictingInfo()
        {
            Assert.Throws<ArgumentException>(() => new MfaEnrollment(new GetAccountInfoResponse.MfaEnrollment()
            {
                MfaEnrollmentId = "testId",
                PhoneInfo = "+10987654321",
                TotpInfo = new(),
            }));
        }

        [Fact]
        public void ValidPhoneFactor()
        {
            var enrollment = new MfaEnrollment(new GetAccountInfoResponse.MfaEnrollment()
            {
                MfaEnrollmentId = "testId",
                PhoneInfo = "+10987654321",
                EnrolledAt = DateTime.Parse("2014-10-03T15:01:23Z"),
            });

            Assert.Equal("testId", enrollment.MfaEnrollmentId);
            Assert.Equal("+10987654321", enrollment.PhoneInfo);
            Assert.Equal(DateTime.Parse("2014-10-03T15:01:23Z"), enrollment.EnrolledAt);
            Assert.Equal(MfaFactorIdType.Phone, enrollment.MfaFactorId);
        }

        [Fact]
        public void ValidTotpFactor()
        {
            var enrollment = new MfaEnrollment(new GetAccountInfoResponse.MfaEnrollment()
            {
                MfaEnrollmentId = "testId",
                TotpInfo = new(),
                EnrolledAt = DateTime.Parse("2014-10-03T15:01:23Z"),
            });

            Assert.Equal("testId", enrollment.MfaEnrollmentId);
            Assert.Equal(DateTime.Parse("2014 - 10 - 03T15:01:23Z"), enrollment.EnrolledAt);
            Assert.Equal(MfaFactorIdType.Totp, enrollment.MfaFactorId);
        }
    }
}
