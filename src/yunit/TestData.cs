// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Yunit
{
    public class TestData
    {
        /// <summary>
        /// Gets the absolute path of the declaring file path.
        /// <summary>
        public string FilePath { get; internal set; }

        /// <summary>
        /// Gets the one based start line number in the declaring file.
        /// <summary>
        public int LineNumber { get; internal set; }

        /// <summary>
        /// Gets the one based ordinal in the declaring file.
        /// <summary>
        public int Ordinal { get; internal set; }

        /// <summary>
        /// Gets the summary of this data fragment.
        /// <summary>
        public string Summary { get; internal set; }

        /// <summary>
        /// Gets the markdown fenced code tip. E.g. yml for ````yml
        /// <summary>
        public string FenceTip { get; internal set; }

        /// <summary>
        /// Gets the content of this data fragment.
        /// <summary>
        public string Content { get; internal set; }

        /// <summary>
        /// Gets the expanded metrix name.
        /// </summary>
        public string Matrix { get; internal set; }

        /// <summary>
        /// Gets or sets whether the source YAML fragment should be updated if a test returns an object.
        /// </summary>
        public bool UpdateSource { get; internal set; }

        internal TestData Clone()
        {
            return (TestData)MemberwiseClone();
        }
    }
}
