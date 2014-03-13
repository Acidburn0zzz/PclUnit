﻿// 
//  Copyright 2013 PclUnit Contributors
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using PclUnit.Run;
using PclUnit.Util;
namespace pclunit_runner
{
    public static class PrintResults
    {
        public static bool TeamCity;
        public static bool AppVeyor;

        private static Uri _appVeyorBaseUri = null;
        private static void PostToAppVeyor(string json)
        {
            try
            {
                if (_appVeyorBaseUri == null)
                {
                    _appVeyorBaseUri = new Uri(Environment.GetEnvironmentVariable("APPVEYOR_API_URL"));
                }

                var url = new Uri(_appVeyorBaseUri, "api/tests");
                Byte[] byteArray = Encoding.UTF8.GetBytes(json);

                var request = WebRequest.Create(url);
                request.Method = "POST";
                request.ContentLength = byteArray.Length;
                request.ContentType = @"application/json";

                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }
                
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    //Don't care
                }
            }
            catch 
            {
                //Don't care
            }
        }


        public static void PrintStart()
        {
           
            if (TeamCity)
            {
                Console.WriteLine("##teamcity[testSuiteStarted name='{0}']", "all");
            }
            else
            {
                Console.WriteLine("Starting Tests");
            }
        }

        public static void PrintEnd(PlatformResults results)
        {
            var file = results.File;

            if (TeamCity)
            {
                Console.WriteLine("##teamcity[testSuiteStarted name='{0}']", "all");
            }
            else
            {

                PrintCount(results.Errors, "Errors:");
                PrintCount(results.Failures, "Failures:");
                PrintCount(results.Ignores, "Ignores:");
                PrintCount(results.NoErrors, "NoErrors:");
                PrintTotaledCount(results, results.Success, "Success:");

                Console.WriteLine("Final Total");
                Console.WriteLine();
                var resultCount = file.ResultCount;
                foreach (var kp in resultCount.OrderBy(it => it.Key))
                {
                    Console.WriteLine("  {0,-15}{1,4}", kp.Key, kp.Value);
                }
                Console.WriteLine("{0,-17}{1,4}", "Total", resultCount.Select(r => r.Value).Sum());
            }
        }


        public static void PrintTotaledCount(PlatformResults fullResults, IEnumerable<Result> results, string header)
        {

            lock (fullResults.ExpectedTests)
            {
                Console.WriteLine(header);
                var totalCount = fullResults.ExpectedTests.SelectMany(it => it.Value)
                                               .Select(it => it.Result)
                                               .ToLookup(it => it.Platform)
                                               .ToDictionary(k => k.Key, v => v.Count());
                foreach (var result in results.GroupBy(it => it.Platform))
                {
                    Console.Write("  {0,-15}{1,6}/{2}" ,
                        result.Key,
                        result.Count(), 
                        totalCount.ContainsKey(result.Key) 
                            ? totalCount[result.Key]
                            : 0
                        );
                    Console.WriteLine();
                }

                Console.WriteLine();
            }
        }

        public static void PrintCount(IEnumerable<Result> results, string header)
        {
            if (results.Any())
            {
                Console.WriteLine(header);
                foreach (var result in results.GroupBy(it => it.Platform))
                {
                    Console.Write("  {0,-15}{1,6}",
                         result.Key,
                         result.Count()
                         ); 
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }


        public static string TeamCityEncode(this string value)
        {
            value = value.Replace("|", "||");
            value = value.Replace("'", "|'");
            value = value.Replace("\n", "|n");
            value = value.Replace("\r", "|r");
            value = value.Replace("[", "|[");
            value = value.Replace("]", "|]");
            return value;
        }

        public static void PrintResult(IDictionary<string, PlatformResult> dict)
        {

            if (AppVeyor)
            {
                foreach (var result in dict.Values.Select(it=>it.Result).Where(it=>it !=null))
                {

                    PostTestResultToAppveyor(result);
                }


            }


            if (dict.All(it => it.Value.Result != null))
            {
                var result = dict.Select(it => it.Value.Result).First();
                if (TeamCity)
                {
                    Console.WriteLine("##teamcity[testStarted name='{2}.{1}.{0}' captureStandardOutput='true']",
                                      result.Test.Name.TeamCityEncode(), 
                                      result.Test.Fixture.Name.TeamCityEncode(),
                                      result.Test.Fixture.Assembly.Name.TeamCityEncode());
                }
                else
                {
                    Console.Write(result.Test.Fixture.Assembly.Name + ".");
                    Console.Write(result.Test.Fixture.Name + ".");
                }
                Console.WriteLine(result.Test.Name);
                foreach (var grpResult in dict.GroupBy(it => it.Value.Result.Kind))
                {
                    Console.Write("{0}:", grpResult.Key);
                    foreach (var keyValuePair in grpResult)
                    {
                        Console.Write(" ");
                        Console.Write(keyValuePair.Value.Platform);
                    }
                    if (TeamCity)
                    {
                        switch (grpResult.Key)
                        {
                            case ResultKind.Fail:
                            case ResultKind.Error:
                                Console.WriteLine(
                                    "##teamcity[testFailed name='{2}.{1}.{0}' message='See log or details']",
                                      result.Test.Name.TeamCityEncode(),
                                      result.Test.Fixture.Name.TeamCityEncode(),
                                      result.Test.Fixture.Assembly.Name.TeamCityEncode());
                                break;
                            case ResultKind.Ignore:
                                Console.WriteLine(
                                    "##teamcity[testIgnored name='{2}.{1}.{0}' message='See log or details']",
                                      result.Test.Name.TeamCityEncode(),
                                      result.Test.Fixture.Name.TeamCityEncode(),
                                      result.Test.Fixture.Assembly.Name.TeamCityEncode());
                                break;
                        }

                    }
                    Console.WriteLine();
                }
                var span = new TimeSpan();
                foreach (var r in dict.Select(it => it.Value.Result))
                {
                    span += (r.EndTime - r.StartTime);
                }
                Console.WriteLine("avg time:{0}", new TimeSpan(span.Ticks / dict.Count));


                foreach (var lup in dict.ToLookup(it => it.Value.Result.Output))
                {
                    var name = String.Join(",", lup.Select(it => it.Value.Platform));

                    Console.WriteLine("{0}:", name);
                    Console.WriteLine(lup.Key);
                }

                if (TeamCity)
                {
                    Console.WriteLine("##teamcity[testFinished name='{2}.{1}.{0}' duration='{3}']",
                         result.Test.Name.TeamCityEncode(), 
                         result.Test.Fixture.Name.TeamCityEncode(),
                         result.Test.Fixture.Assembly.Name.TeamCityEncode(),
                        (result.EndTime - result.StartTime).TotalMilliseconds);
                }
                else
                {

                    if (dict.Any(it => it.Value.Result.Kind == ResultKind.Fail))
                    {
                        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!");
                    }
                    else if (dict.Any(it => it.Value.Result.Kind == ResultKind.Error))
                    {
                        Console.WriteLine("EEEEEEEEEEEEEEEEEEEEEEEEE");
                    }
                    else if (dict.Any(it => it.Value.Result.Kind == ResultKind.Ignore))
                    {
                        Console.WriteLine("?????????????????????????");
                    }
                    else if (dict.All(it => it.Value.Result.Kind == ResultKind.Success))
                    {
                        Console.WriteLine("-------------------------");
                    }
                    else
                    {
                        Console.WriteLine(".........................");
                    }

                    Console.WriteLine(String.Empty);
                }
            }
        }

        private static void PostTestResultToAppveyor(Result result)
        {
            var fullName = string.Format("{0}.{1}.{2}", result.Test.Name, result.Test.Fixture.Name,
                                         result.Test.Fixture.Assembly.Name);
            var testFramework = string.Format("PclUnit[{0}]", result.Platform);


            string outcome = null;
            switch (result.Kind)
            {
                case ResultKind.Success:
                    outcome = "Passed";
                    break;
                case ResultKind.Fail:
                    outcome = "Failed";
                    break;
                case ResultKind.Error:
                    outcome = "NotRunnable";
                    break;
                case ResultKind.Ignore:
                    outcome = "Ignored";
                    break;
                case ResultKind.NoError:
                    outcome = "Inconclusive";
                    break;
            }


            var json = string.Format(@"{{
                                        'testName': '{0}',
                                        'testFramework': '{1}',
                                        'fileName': '{2}',
                                        'outcome': '{3}',
                                        'durationMilliseconds': '{4}',
                                        'ErrorMessage': '',
                                        'ErrorStackTrace': '',
                                        'StdOut': '{5}',
                                        'StdErr': ''  
                                    }}",
                                     fullName.EscapeJson(),
                                     testFramework.EscapeJson(),
                                     result.Test.Fixture.Assembly.Name.EscapeJson(),
                                     outcome.EscapeJson(),
                                     (result.EndTime - result.StartTime).Milliseconds,
                                     result.Output.EscapeJson()
                );

            PostToAppVeyor(json);
        }
    }
}
