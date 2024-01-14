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
        private int _enqueuePosition;

        /// <summary>
        /// The current enqueue position (length) of the queue.
        /// </summary>
        public int EnqueuePosition => _enqueuePosition;
        /// <summary>
        /// The underlying data array representing the queue.
        /// </summary>
        public T[] Data => _voiceDataBuffer;

        /// <summary>
        /// Constructs a voice data queue.
        /// </summary>
        /// <param name="defaultLength">Starting length of the underlying array</param>
        public VoiceDataQueue(uint defaultLength)
        {
            _enqueuePosition = 0;
            _voiceDataBuffer = new T[defaultLength];
        }
        
        /// <summary>
        /// Constructs a voice data queue.
        /// </summary>
        /// <param name="defaultLength">Starting length of the underlying array</param>
        public VoiceDataQueue(int defaultLength)
        {
            _enqueuePosition = 0;
            _voiceDataBuffer = new T[defaultLength];
        } 

        /// <summary>
        /// Enqueues a span of data by copying it into the underlying array.
        /// </summary>
        /// <param name="voiceData">Span of data</param>
        public void Enqueue(Span<T> voiceData)
        {
            ResizeIfNeeded(voiceData.Length);
            // Copy voice data over
            voiceData.CopyTo( new Span<T> (_voiceDataBuffer).Slice(_enqueuePosition, voiceData.Length));
            _enqueuePosition += voiceData.Length;
        }

        /// <summary>
        /// Removes specified amount of data from the front of the queue.
        /// </summary>
        /// <param name="dequeueCount">Amount of data to dequeue</param>
        /// <exception cref="ArgumentOutOfRangeException">Dequeue count must
        /// be less than or equal to the current <see cref="EnqueuePosition"/> of the queue</exception>
        public void Dequeue(int dequeueCount)
        {
            if (dequeueCount > _enqueuePosition)
                throw new ArgumentOutOfRangeException("Attempted to dequeue more data than exists.");
            
            // Skip copy if we're dequeuing everything
            if (dequeueCount != _enqueuePosition)
                Array.Copy(_voiceDataBuffer, dequeueCount, _voiceDataBuffer, 0, _enqueuePosition - dequeueCount);
            _enqueuePosition -= dequeueCount;
        }

        /// <summary>
        /// Resizes the underlying buffer if the additional data would overflow it.
        /// </summary>
        /// <param name="additionalDataCount">Amount of additional data the buffer needs to be able to store</param>
        public void ResizeIfNeeded(int additionalDataCount)
        {
            // No resize needed if we already have space for this additional data
            if (_voiceDataBuffer.Length - _enqueuePosition >= additionalDataCount) return;
            // Otherwise resize to fit current and additional data
            Array.Resize(ref _voiceDataBuffer, Mathf.Max(_voiceDataBuffer.Length * 2, _enqueuePosition + additionalDataCount));
        }

        /// <summary>
        /// Manually modify the write position of the underlying data buffer.
        /// Useful when manually writing data to the buffer without using enqueue.
        /// </summary>
        /// <param name="deltaWritePosition">Change in write position</param>
        /// <exception cref="IndexOutOfRangeException">Thrown  if write position modified
        /// outside bounds of underlying array</exception>
        public void ModifyWritePosition(int deltaWritePosition)
        {
            _enqueuePosition += deltaWritePosition;

            if (_enqueuePosition < 0 || _enqueuePosition >= _voiceDataBuffer.Length)
                throw new IndexOutOfRangeException("Write position modified to an illegal position.");
        }
    }
}
