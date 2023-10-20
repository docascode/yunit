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
    public class ParallelLevelTest
    {
        static volatile bool s_isAllParallelLevelTestRunning = false;

        static readonly ConcurrentDictionary<string, DateTime> s_fileParallelLevelTestsStartTime = new();
        static readonly ConcurrentDictionary<string, DateTime> s_fileParallelLevelTestsEndTime = new();

        [MarkdownTest("~/test/yunit.test/yunit.test.nonparallel.md", ParallelLevel = ParallelLevel.None)]
        public async Task TestWithAllParallelLevel()
        {
            if (s_isAllParallelLevelTestRunning)
            {
                throw new InvalidOperationException("Test is already running.");
            }
            s_isAllParallelLevelTestRunning = true;
            await Task.Delay(1000);
            s_isAllParallelLevelTestRunning = false;
        }

        [MarkdownTest("~/test/yunit.test/yunit.test.fileparallel*.md", ParallelLevel = ParallelLevel.File)]
        public async Task TestWithFileParallelLevel(TestData data)
        {
            string path = data.FilePath;
            var fileTestsStartTime = s_fileParallelLevelTestsStartTime.GetOrAdd(path, DateTime.UtcNow);
            await Task.Delay(5000);
            s_fileParallelLevelTestsEndTime.AddOrUpdate(path, _ => DateTime.UtcNow, (_, _) => DateTime.UtcNow);
            if (s_fileParallelLevelTestsStartTime.Any(dic => dic.Key != path && dic.Value > fileTestsStartTime)
                || s_fileParallelLevelTestsEndTime.Any(dic => dic.Key != path && dic.Value > fileTestsStartTime))
            {
                throw new InvalidOperationException("Tests of other files are running.");
            }
        }
    }
}
