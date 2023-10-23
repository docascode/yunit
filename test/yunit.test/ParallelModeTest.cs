using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Yunit;

namespace yunit.test
{
    public class ParallelModeTest
    {
        static volatile bool s_isParallelModeTestRunning = false;

        static readonly ConcurrentDictionary<string, DateTime> s_fileSequentialTestsParallelModeTestsStartTime = new();
        static readonly ConcurrentDictionary<string, DateTime> s_fileSequentialTestsParallelModeTestsEndTime = new();

        static readonly ConcurrentDictionary<string, bool> s_isFileSequentialTestsParallelModeTestsRunning = new();

        [MarkdownTest("~/test/yunit.test/yunit.test.parallelmode.1.md", ParallelMode = ParallelMode.Sequential)]
        public async Task TestWithParallelMode()
        {
            if (s_isParallelModeTestRunning)
            {
                throw new InvalidOperationException("[Sequential]Test is already running.");
            }
            s_isParallelModeTestRunning = true;
            await Task.Delay(1000);
            s_isParallelModeTestRunning = false;
        }

        [MarkdownTest("~/test/yunit.test/yunit.test.parallelmode*.md", ParallelMode = ParallelMode.FileSequentialTestsParallel)]
        public async Task TestWithFileSequentialTestsParallelMode(TestData data)
        {
            string path = data.FilePath;
            var fileTestsStartTime = s_fileSequentialTestsParallelModeTestsStartTime.GetOrAdd(path, DateTime.UtcNow);
            await Task.Delay(1000);
            s_fileSequentialTestsParallelModeTestsEndTime.AddOrUpdate(path, _ => DateTime.UtcNow, (_, _) => DateTime.UtcNow);
            if (s_fileSequentialTestsParallelModeTestsStartTime.Any(dic => dic.Key != path && dic.Value > fileTestsStartTime)
                || s_fileSequentialTestsParallelModeTestsEndTime.Any(dic => dic.Key != path && dic.Value > fileTestsStartTime))
            {
                throw new InvalidOperationException("[FileSequentialTestsParallel]Tests of other files are running.");
            }
        }

        [MarkdownTest("~/test/yunit.test/yunit.test.parallelmode*.md", ParallelMode = ParallelMode.FileParallelTestsSequential)]
        public async Task TestWithFileParallelTestsSequentialMode(TestData data)
        {
            string path = data.FilePath;
            s_isFileSequentialTestsParallelModeTestsRunning.TryGetValue(path, out var isRunning);
            if (isRunning)
            {
                throw new InvalidOperationException("[FileParallelTestsSequential]Test is already running.");
            }
            s_isFileSequentialTestsParallelModeTestsRunning[path] = true;
            await Task.Delay(1000);
            s_isFileSequentialTestsParallelModeTestsRunning[path] = false;
        }
    }
}
