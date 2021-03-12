// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Yunit
{
    internal interface ITestAttribute
    {
        string Glob { get; }

        string ExpandTest { get; }

        int Timeout { get; }

        void DiscoverTests(string path, Action<TestData> report);
    }
}
