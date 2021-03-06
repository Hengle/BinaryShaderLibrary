﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Syroot.BinaryData;
using Syroot.Maths;

namespace BfshaLibrary.Core
{
    /// <summary>
    /// Loads the hierachy and data of a <see cref="Bfsha.BfshaFile"/>.
    /// </summary>
    public class BfshaFileLoader : BinaryDataReader
    {
        // ---- FIELDS -------------------------------------------------------------------------------------------------

        private IDictionary<uint, IResData> _dataMap;

        // ---- CONSTRUCTORS & DESTRUCTOR ------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="BfshaFileLoader"/> class loading data into the given
        /// <paramref name="bfshaFile"/> from the specified <paramref name="stream"/> which is optionally left open.
        /// </summary>
        /// <param name="bfshaFile">The <see cref="Bfsha.bfshaFile"/> instance to load data into.</param>
        /// <param name="stream">The <see cref="Stream"/> to read data from.</param>
        /// <param name="leaveOpen"><c>true</c> to leave the stream open after reading, otherwise <c>false</c>.</param>
        internal BfshaFileLoader(BfshaFile bfshaFile, Stream stream, bool leaveOpen = false)
            : base(stream, Encoding.ASCII, leaveOpen)
        {
            ByteOrder = ByteOrder.LittleEndian;
            BfshaFile = bfshaFile;
            _dataMap = new Dictionary<uint, IResData>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BfshaFileLoader"/> class loading data into the given
        /// <paramref name="bfshaFile"/> from the specified <paramref name="stream"/> which is optionally left open.
        /// </summary>
        /// <param name="bfshaFile">The <see cref="Bfsha.bfshaFile"/> instance to load data into.</param>
        /// <param name="stream">The <see cref="Stream"/> to read data from.</param>
        /// <param name="leaveOpen"><c>true</c> to leave the stream open after reading, otherwise <c>false</c>.</param>
        internal BfshaFileLoader(BnshFile bnshFile, Stream stream, bool leaveOpen = false)
            : base(stream, Encoding.ASCII, leaveOpen)
        {
            ByteOrder = ByteOrder.LittleEndian;
            BnshFile = bnshFile;
            _dataMap = new Dictionary<uint, IResData>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BfshaFileLoader"/> class from the file with the given
        /// <paramref name="fileName"/>.
        /// </summary>
        /// <param name="BfshaFile">The <see cref="Bfsha.BfshaFile"/> instance to load data into.</param>
        /// <param name="fileName">The name of the file to load the data from.</param>
        internal BfshaFileLoader(BfshaFile BfshaFile, string fileName)
            : this(BfshaFile, new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BfshaFileLoader"/> class from the file with the given
        /// <paramref name="fileName"/>.
        /// </summary>
        /// <param name="BfshaFile">The <see cref="Bfsha.BfshaFile"/> instance to load data into.</param>
        /// <param name="fileName">The name of the file to load the data from.</param>
        internal BfshaFileLoader(BnshFile BnshFile, string fileName)
            : this(BnshFile, new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
        }

        // ---- PROPERTIES ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the loaded <see cref="Bfsha.BfshaFile"/> instance.
        /// </summary>
        internal BfshaFile BfshaFile { get; }

        /// <summary>
        /// Gets the loaded <see cref="Bfsha.BnshFile"/> instance.
        /// </summary>
        internal BnshFile BnshFile { get; }

        // ---- METHODS (INTERNAL) -------------------------------------------------------------------------------------

        /// <summary>
        /// Starts deserializing the data from the <see cref="BfshaFile"/> root.
        /// </summary>
        internal void Execute()
        {
            // Load the raw data into structures recursively.
            if (BfshaFile != null)
                ((IResData)BfshaFile).Load(this);
            if (BnshFile != null)
                ((IResData)BnshFile).Load(this);
        }

        /// <summary>
        /// Reads and returns an <see cref="IResData"/> instance of type <typeparamref name="T"/> from the following
        /// offset or returns <c>null</c> if the read offset is 0.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IResData"/> to read.</typeparam>
        /// <returns>The <see cref="IResData"/> instance or <c>null</c>.</returns>
        [DebuggerStepThrough]
        internal T LoadSection<T>()
            where T : IResData, new()
        {
            T instance = new T();
            instance.Load(this);
            return instance;
        }

        /// <summary>
        /// Reads and returns an <see cref="IResData"/> instance of type <typeparamref name="T"/> from the following
        /// offset or returns <c>null</c> if the read offset is 0.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IResData"/> to read.</typeparam>
        /// <returns>The <see cref="IResData"/> instance or <c>null</c>.</returns>
        [DebuggerStepThrough]
        internal T Load<T>(long offset)
            where T : IResData, new()
        {
            if (offset == 0) return default(T);

            // Seek to the instance data and load it.
            using (TemporarySeek(offset, SeekOrigin.Begin))
            {
                return ReadResData<T>();
            }
        }

        /// <summary>
        /// Reads and returns an <see cref="IResData"/> instance of type <typeparamref name="T"/> from the following
        /// offset or returns <c>null</c> if the read offset is 0.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IResData"/> to read.</typeparam>
        /// <returns>The <see cref="IResData"/> instance or <c>null</c>.</returns>
        [DebuggerStepThrough]
        internal T Load<T>(bool Relocated = false)
            where T : IResData, new()
        {
            long offset = ReadOffset();
            if (offset == 0) return default(T);

            // Seek to the instance data and load it.
            using (TemporarySeek(offset, SeekOrigin.Begin))
            {
                return ReadResData<T>();
            }
        }

        /// <summary>
        /// Reads and returns an instance of arbitrary type <typeparamref name="T"/> from the following offset with the
        /// given <paramref name="callback"/> or returns <c>null</c> if the read offset is 0.
        /// </summary>
        /// <typeparam name="T">The type of the data to read.</typeparam>
        /// <param name="callback">The callback to read the instance data with.</param>
        /// <param name="offset">The optional offset to use instead of reading a following one.</param>
        /// <returns>The data instance or <c>null</c>.</returns>
        /// <remarks>Offset required for ExtFile header (offset specified before size).</remarks>
        [DebuggerStepThrough]
        internal T LoadCustom<T>(Func<T> callback, long? offset = null)
        {
            offset = offset ?? ReadOffset();
            if (offset == 0) return default(T);

            using (TemporarySeek(offset.Value, SeekOrigin.Begin))
            {
                return callback.Invoke();
            }
        }

        /// <summary>
        /// Reads and returns an <see cref="ResDict{T}"/> instance with elements of type <typeparamref name="T"/> from
        /// the following offset or returns an empty instance if the read offset is 0.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IResData"/> elements.</typeparam>
        /// <returns>The <see cref="ResDict{T}"/> instance.</returns>
        [DebuggerStepThrough]
        internal ResDict LoadDict()
        {
            long offset = ReadInt64();
            if (offset == 0) return new ResDict();

            using (TemporarySeek(offset, SeekOrigin.Begin))
            {
                ResDict dict = new ResDict();
                ((IResData)dict).Load(this);
                return dict;
            }
        }

        /// <summary>
        /// Reads and returns an <see cref="IList{T}"/> instance with <paramref name="count"/> elements of type
        /// <typeparamref name="T"/> from the following offset or returns <c>null</c> if the read offset is 0.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IResData"/> elements.</typeparam>
        /// <param name="count">The number of elements to expect for the list.</param>
        /// <param name="offset">The optional offset to use instead of reading a following one.</param>
        /// <returns>The <see cref="IList{T}"/> instance or <c>null</c>.</returns>
        /// <remarks>Offset required for FMDL FVTX lists (offset specified before count).</remarks>
        [DebuggerStepThrough]
        internal List<T> LoadList<T>(int count, long? offset = null)
            where T : IResData, new()
        {
            List<T> list = new List<T>(count);
            offset = offset ?? ReadOffset();
            if (offset == 0 || count == 0) return list;

            // Seek to the list start and read it.
            using (TemporarySeek(offset.Value, SeekOrigin.Begin))
            {
                for (; count > 0; count--)
                {
                    list.Add(ReadResData<T>());
                }
                return list;
            }
        }

        /// <summary>
        /// Reads and returns a <see cref="String"/> instance from the following offset or <c>null</c> if the read
        /// offset is 0.
        /// </summary>
        /// <param name="encoding">The optional encoding of the text.</param>
        /// <returns>The read text.</returns>
        [DebuggerStepThrough]
        internal string LoadString(Encoding encoding = null)
        {
            long offset = ReadOffset();
            if (offset == 0) return null;

            encoding = encoding ?? Encoding;
            using (TemporarySeek(offset, SeekOrigin.Begin))
            {
                short size = ReadInt16();
                return ReadString(BinaryStringFormat.ZeroTerminated, encoding);
            }
        }

        /// <summary>
        /// Reads and returns <paramref name="count"/> <see cref="String"/> instances from the following offsets.
        /// </summary>
        /// <param name="count">The number of instances to read.</param>
        /// <param name="encoding">The optional encoding of the texts.</param>
        /// <returns>The read texts.</returns>
        [DebuggerStepThrough]
        internal List<string> LoadStrings(int count, Encoding encoding = null)
        {
            long[] offsets = ReadOffsets(count);

            encoding = encoding ?? Encoding;
            string[] names = new string[offsets.Length];
            using (TemporarySeek())
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    long offset = offsets[i];
                    if (offset == 0) continue;

                    Position = offset;
                    short size = ReadInt16();
                    names[i] = ReadString(BinaryStringFormat.ZeroTerminated, encoding);
                }
                return new List<string>(names);
            }
        }
        
        /// <summary>
        /// Reads a Bfsha signature consisting of 4 ASCII characters encoded as an <see cref="UInt32"/> and checks for
        /// validity.
        /// </summary>
        /// <param name="validSignature">A valid signature.</param>
        internal void CheckSignature(string validSignature)
        {
            // Read the actual signature and compare it.
            string signature = ReadString(sizeof(uint), Encoding.ASCII);
            if (signature != validSignature)
            {
                throw new ResException($"Invalid signature, expected '{validSignature}' but got '{signature}'.");
            }
        }

        /// <summary>
        /// Reads a Bfsha offset which is the absolute address.
        /// </summary>
        /// <returns>The absolute address of the offset.</returns>
        internal long ReadOffset(bool Relocated = false)
        {
            long offset = ReadInt64();

            return offset == 0 ? 0 : offset;
        }

        /// <summary>
        /// Reads Bfsha offsets which is the absolute addresses.
        /// </summary>
        /// <param name="count">The number of offsets to read.</param>
        /// <returns>The absolute addresses of the offsets.</returns>
        internal long[] ReadOffsets(int count)
        {
            long[] values = new long[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = ReadOffset();
            }
            return values;
        }

        // ---- METHODS (PRIVATE) --------------------------------------------------------------------------------------
        
        [DebuggerStepThrough]
        private T ReadResData<T>()
            where T : IResData, new()
        {
            uint offset = (uint)Position;

            // Same data can be referenced multiple times. Load it in any case to move in the stream, needed for lists.
            T instance = new T();
            instance.Load(this);

            // If possible, return an instance already representing the data.
            if (_dataMap.TryGetValue(offset, out IResData existingInstance))
            {
                return (T)existingInstance;
            }
            else
            {
                _dataMap.Add(offset, instance);
                return instance;
            }
        }
    }
}
