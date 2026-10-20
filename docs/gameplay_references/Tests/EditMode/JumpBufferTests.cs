using NUnit.Framework;

namespace SpiderManRebuild.Tests
{
    public sealed class JumpBufferTests
    {
        [Test]
        public void PressIsAvailableOnceWithinWindow()
        {
            var buffer = new JumpBuffer();
            Assert.That(buffer.TryConsume(10), Is.False);
            buffer.Press(10, .14);
            Assert.That(buffer.TryConsume(10.1), Is.True);
            Assert.That(buffer.TryConsume(10.11), Is.False);
        }
        [Test]
        public void ExpiredInputCannotJumpOnLaterLanding()
        {
            var buffer = new JumpBuffer();
            buffer.Press(10, .14);
            Assert.That(buffer.TryConsume(10.15), Is.False);
        }
        [Test]
        public void ResetAndPauseCanDiscardQueuedJump()
        {
            var buffer = new JumpBuffer();
            buffer.Press(10, .14);
            buffer.Clear();
            Assert.That(buffer.TryConsume(10.01), Is.False);
        }
    }
}
