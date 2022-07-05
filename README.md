# ParentageAnalysisOH.NET

[![](https://img.shields.io/nuget/v/ParentageAnalysisOH.NET.svg)](https://www.nuget.org/packages/ParentageAnalysisOH.NET/)
[![](https://img.shields.io/nuget/dt/ParentageAnalysisOH.NET.svg)](https://www.nuget.org/packages/ParentageAnalysisOH.NET/)

A library for parentage anlaysis using efficiently computed opposing homozygote counts of a sample set.

## Prerequisites

- Git
- Git LFS (Large File Storage)[^1]
- An application to run .NET 6 projects[^2]

[^1]: The repository contains large files, so the Git extension Git LFS (Large File Storage) must be installed to clone the repository locally. GitHub Desktop, and some other Git GUI applications, has Git and Git LFS integrated; this is perhaps the easiest way to clone the repository.
[^2]: The repository is primarily created with Visual Studio 2022 in mind.

## Benchmark

This sections describes how to run the benchmarking project `BenchmarkConsoleApp`.

### Setup

1. Extract the files from the ZIP archive at `ParentageAnalysisOH.NET\BenchmarkConsoleApp` into the `ParentageAnalysisOH.NET\BenchmarkConsoleApp\Datasets` folder before running the benchmarking application.
2. Make sure that the benchmarking application runs in Release mode.

### Configuration
- Select which data set to run benchmarks on by un-/commenting the approprate lines in the constructor of the `OppositeHomozygoteBenchmarks` class.
- Disable specific benchmarks by commenting out the appropriate `[Benchmark]` tags.

