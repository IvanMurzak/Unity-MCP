/*
+-----------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.  |
+-----------------------------------------------------------------+
*/

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("com.IvanMurzak.Unity.MCP.Editor.Tests")]
// tests/filter-oracle: checks the tests-run filter count against what Unity runs (CI, filter_oracle.yml).
[assembly: InternalsVisibleTo("com.IvanMurzak.Unity.MCP.FilterOracle")]
