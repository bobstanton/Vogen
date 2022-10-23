﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using MediumTests.DiagnosticsTests;
using Microsoft.CodeAnalysis;
using VerifyTests;
using VerifyXunit;

namespace MediumTests.SnapshotTests
{
    public class SnapshotRunner<T> where T : IIncrementalGenerator, new()
    {
        public SnapshotRunner([CallerFilePath]string caller = "")
        {
            int n = caller.LastIndexOf('\\');
            n = n > 0 ? n : caller.LastIndexOf('/');
            _path = caller.Substring(0, n);
        }

        private readonly TargetFramework[] _allFrameworks = new[]
        {
            TargetFramework.Net6_0,
            TargetFramework.Net7_0,
        };

        public SnapshotRunner<T> WithLocale(string locale)
        {
            _locale = locale;
            return this;
        }

        private string? _source;
        private readonly string _path;
        private Action<VerifySettings>? _customizesSettings;
        
        private string _locale = string.Empty;

        public async Task RunOnAllFrameworks() => await RunOn(_allFrameworks);

        public SnapshotRunner<T> WithSource(string source)
        {
            _source = source;
            return this;
        }

        public SnapshotRunner<T> CustomizeSettings(Action<VerifySettings> settings)
        {
            _customizesSettings = settings;
            return this;
        }

        public async Task RunOn(params TargetFramework[] frameworks)
        {
            _ = _source ?? throw new InvalidOperationException("No source!");

            foreach (var eachFramework in frameworks)
            {
                VerifySettings? verifySettings = null;

                if (_customizesSettings is not null)
                {
                    verifySettings = new();
                    _customizesSettings(verifySettings);
                }

                using var scope = new AssertionScope();

                var (diagnostics, output) = TestHelper.GetGeneratedOutput<T>(_source, eachFramework);
                diagnostics.Should().BeEmpty();

                var outputFolder = Path.Combine(_path, SnapshotUtils.GetSnapshotDirectoryName(eachFramework, _locale));

                await Verifier.Verify(output, verifySettings).UseDirectory(outputFolder);
            }
        }
    }
}