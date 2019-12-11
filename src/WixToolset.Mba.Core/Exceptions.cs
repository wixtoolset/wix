// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base class for exception returned to the bootstrapper application host.
    /// </summary>
    [Serializable]
    public abstract class BootstrapperException : Exception
    {
        /// <summary>
        /// Creates an instance of the <see cref="BootstrapperException"/> base class with the given HRESULT.
        /// </summary>
        /// <param name="hr">The HRESULT for the exception that is used by the bootstrapper application host.</param>
        public BootstrapperException(int hr)
        {
            this.HResult = hr;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BootstrapperException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public BootstrapperException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BootstrapperException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception associated with this one</param>
        public BootstrapperException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BootstrapperException"/> class.
        /// </summary>
        /// <param name="info">Serialization information for this exception</param>
        /// <param name="context">Streaming context to serialize to</param>
        protected BootstrapperException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The bootstrapper application assembly loaded by the host does not contain exactly one instance of the
    /// <see cref="BootstrapperApplicationFactoryAttribute"/> class.
    /// </summary>
    /// <seealso cref="BootstrapperApplicationFactoryAttribute"/>
    [Serializable]
    public class MissingAttributeException : BootstrapperException
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MissingAttributeException"/> class.
        /// </summary>
        public MissingAttributeException()
            : base(NativeMethods.E_NOTFOUND)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingAttributeException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public MissingAttributeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingAttributeException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception associated with this one</param>
        public MissingAttributeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingAttributeException"/> class.
        /// </summary>
        /// <param name="info">Serialization information for this exception</param>
        /// <param name="context">Streaming context to serialize to</param>
        protected MissingAttributeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The bootstrapper application factory specified by the <see cref="BootstrapperApplicationFactoryAttribute"/>
    ///  does not extend the <see cref="IBootstrapperApplicationFactory"/> base class.
    /// </summary>
    /// <seealso cref="BaseBootstrapperApplicationFactory"/>
    /// <seealso cref="BootstrapperApplicationFactoryAttribute"/>
    [Serializable]
    public class InvalidBootstrapperApplicationFactoryException : BootstrapperException
    {
        /// <summary>
        /// Creates a new instance of the <see cref="InvalidBootstrapperApplicationFactoryException"/> class.
        /// </summary>
        public InvalidBootstrapperApplicationFactoryException()
            : base(NativeMethods.E_UNEXPECTED)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidBootstrapperApplicationFactoryException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public InvalidBootstrapperApplicationFactoryException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidBootstrapperApplicationFactoryException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception associated with this one</param>
        public InvalidBootstrapperApplicationFactoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidBootstrapperApplicationFactoryException"/> class.
        /// </summary>
        /// <param name="info">Serialization information for this exception</param>
        /// <param name="context">Streaming context to serialize to</param>
        protected InvalidBootstrapperApplicationFactoryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
