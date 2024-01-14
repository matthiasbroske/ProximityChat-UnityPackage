using NUnit.Framework;

namespace ProximityChat.Tests
{
    public class VoiceDataQueueTests
    {
        [Test]
        public void Enqueue_SequentialArray_EnqueuesInSequence()
        {
            // Arrange
            VoiceDataQueue<short> voiceDataQueue = new VoiceDataQueue<short>(100);
            short[] dataToEnqueue = new short[] { 1, 2, 3, 4, 5 };
            // Act
            voiceDataQueue.Enqueue(dataToEnqueue);
            // Assert
            Assert.AreEqual(voiceDataQueue.EnqueuePosition, dataToEnqueue.Length);
            for (int i = 0; i < dataToEnqueue.Length; i++)
            {
                Assert.AreEqual(dataToEnqueue[i], voiceDataQueue.Data[i]);
            }
        }

        [Test]
        public void Enqueue_ArrayTooLarge_ResizeQueue()
        {
            // Arrange
            VoiceDataQueue<short> voiceDataQueue = new VoiceDataQueue<short>(10);
            short[] dataToEnqueue = new short[20];
            int lengthBeforeEnqueue = voiceDataQueue.Data.Length;
            // Act
            voiceDataQueue.Enqueue(dataToEnqueue);
            // Assert
            Assert.Greater(voiceDataQueue.Data.Length, lengthBeforeEnqueue);
        }

        [Test]
        public void Dequeue_AllData_EnqueuePositionReset()
        {
            // Arrange
            VoiceDataQueue<short> voiceDataQueue = new VoiceDataQueue<short>(10);
            short[] dataToEnqueue = new short[5];
            // Act
            voiceDataQueue.Enqueue(dataToEnqueue);
            voiceDataQueue.Dequeue(dataToEnqueue.Length);
            // Assert
            Assert.AreEqual(voiceDataQueue.EnqueuePosition, 0);
        }

        [Test]
        public void Dequeue_NotAllData_RemainingDataMovesToFrontOfQueue()
        {
            // Arrange
            VoiceDataQueue<short> voiceDataQueue = new VoiceDataQueue<short>(10);
            short[] dataToEnqueue = new short[] { 1, 2, 3, 4, 5 };
            short[] enqueuedDataWithoutFrontTwo = new short[] { 3, 4, 5 };
            voiceDataQueue.Enqueue(dataToEnqueue);
            // Act
            voiceDataQueue.Dequeue(2);
            // Assert
            for (int i = 0; i < enqueuedDataWithoutFrontTwo.Length; i++)
            {
                Assert.AreEqual(enqueuedDataWithoutFrontTwo[i], voiceDataQueue.Data[i]);
            }
        }
    }
}
