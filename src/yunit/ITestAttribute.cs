// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Yunit
{
    internal interface ITestAttribute
    {
        string Glob { get; }

        string ExpandTest { get; }

        bool UpdateSource { get; }

        void DiscoverTests(string path, Action<TestData> report);
    }
}
