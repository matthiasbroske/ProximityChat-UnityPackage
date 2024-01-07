using System;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// A rudimentary queue that supports enqueuing and dequeuing
    /// spans of data using a resizing array.
    /// </summary>
    public class VoiceDataQueue<T>
    {
        private T[] _voiceDataBuffer;
        private int _writePosition;

        /// <summary>
        /// The current length of the queue.
        /// </summary>
        public int Length => _writePosition;
        /// <summary>
        /// The underlying data array representing the queue.
        /// </summary>
        public T[] Data => _voiceDataBuffer;

        /// <summary>
        /// Constructs a voice data queue.
        /// </summary>
        /// <param name="defaultLength">Starting length of the underlying array</param>
        public VoiceDataQueue(int defaultLength)
        {
            _writePosition = 0;
            _voiceDataBuffer = new T[defaultLength];
        }

        /// <summary>
        /// Enqueues a span of data by copying it into the underlying array.
        /// </summary>
        /// <param name="voiceData">Span of data</param>
        public void Enqueue(Span<T> voiceData)
        {
            // Resize voice data buffer if necessary
            if (_voiceDataBuffer.Length - _writePosition < voiceData.Length)
            {
                Array.Resize(ref _voiceDataBuffer, Mathf.Max(_voiceDataBuffer.Length * 2, _writePosition + voiceData.Length));
            }
            // Copy voice data over
            voiceData.CopyTo( new Span<T> (_voiceDataBuffer).Slice(_writePosition, voiceData.Length));
            _writePosition += voiceData.Length;
        }

        /// <summary>
        /// Removes specified amount of data from the front of the queue.
        /// </summary>
        /// <param name="dequeueCount">Amount of data to dequeue</param>
        /// <exception cref="ArgumentOutOfRangeException">Dequeue count must
        /// be less than or equal to the current <see cref="Length"/> of the queue</exception>
        public void Dequeue(int dequeueCount)
        {
            if (dequeueCount > _writePosition)
                throw new ArgumentOutOfRangeException("Attempted to dequeue more data than exists.");
            
            Array.Copy(_voiceDataBuffer, dequeueCount, _voiceDataBuffer, 0, dequeueCount);
            _writePosition -= dequeueCount;
        }
    }
}
