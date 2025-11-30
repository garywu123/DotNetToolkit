using System;
using DotNetToolkit.General;
using Xunit;

namespace DotNetToolkit.Tests.General
{
    public class GuardResultTests
    {
        [Fact]
        public void Guard_NotNull_WithNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Guard.NotNull(null, "value"));
        }

        [Fact]
        public void Result_Ok_IsSuccessTrueAndValueSet()
        {
            var result = Result<int>.Ok(5);

            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Value);
            Assert.Null(result.Error);
        }

        [Fact]
        public void Result_Fail_IsSuccessFalseAndErrorSet()
        {
            var result = Result<int>.Fail("something failed");

            Assert.False(result.IsSuccess);
            Assert.Equal("something failed", result.Error);
        }
    }
}
