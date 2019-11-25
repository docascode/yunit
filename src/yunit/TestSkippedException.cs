// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace yunit
{
    /// <summary>
    /// Marks the current test as skipped.
    /// </summary>
    public class TestSkippedException : Exception
    {
        public string Reason { get; private set; }

        public TestSkippedException() { }

        public TestSkippedException(string reason)
            : base(reason) => Reason = reason;
    }
}
