### Pcl Unit
Write Once, Test Everywhere

[Portable Class Libraries][pcl] allow you to code to a single dll that can run on multiple .net or .net like platforms. However, if you want to test for these platforms you have to use a testing framework specific for each platform and compile your unit tests specifically for each platform.

The goal of PclUnit is to target the broadest PCL profile and implement both describing tests and executing tests in the profile. To have runner executables specific for each platform which can consolidate results in a clear and consistent way.

See [Pcl Unit Design and Philosophy][Design].

| Status | Provider |
| --- | --- |
| [![Build Status][WinImg]][WinLink] | Windows CI Provided By [CodeBetter][] and [JetBrains][] |
| [![Build Status][MonoImg]][MonoLink] | Mono CI Provided by [travis-ci][] |


[WinImg]:http://teamcity.codebetter.com/app/rest/builds/buildType:(id:bt1048)/statusIcon
[WinLink]:http://teamcity.codebetter.com/viewLog.html?buildTypeId=bt1048&buildId=lastFinished&guest=1
[JetBrains]:http://www.jetbrains.com/
[CodeBetter]:http://codebetter.com/
[MonoImg]:https://travis-ci.org/jbtule/PclUnit.png?branch=master
[MonoLink]:https://travis-ci.org/jbtule/PclUnit

####Status
Alpha Version
  - Core Libraries and Basic Test Runner Only

[travis-ci]:https://travis-ci.org/
[Design]:http://github.com/jbtule/PclUnit/wiki/Design
[pcl]:http://msdn.microsoft.com/en-us/library/gg597391.aspx
