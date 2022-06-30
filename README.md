# ParentageAnalysisOH.NET

[![](https://img.shields.io/nuget/dt/ParentageAnalysisOH.NET.svg)](https://www.nuget.org/packages/ParentageAnalysisOH.NET/) [![](https://img.shields.io/nuget/v/ParentageAnalysisOH.NET.svg)](https://www.nuget.org/packages/ParentageAnalysisOH.NET/)

## Prerequisites

- Git
- Git LFS (Large File Storage)[^1]
- An application to run .NET 6 projects[^2]

[^1]: The repository contains large files, so the Git extension Git LFS (Large File Storage) must be installed to properly clone the repository to a computer. GitHub Desktop, and some other Git GUI applications, has Git and Git LFS integrated; this is one of the easiest ways to clone the repository.
[^2]: The repository is primarily created with Visual Studio 2022 in mind.

## Benchmark

This sections describes how to run the benchmarking project `BenchmarkConsoleApp`.

### Setup

1. Extract the files from the ZIP archive at `ParentageAnalysisOH.NET\BenchmarkConsoleApp` into the `ParentageAnalysisOH.NET\BenchmarkConsoleApp\Datasets` folder before running the benchmarking application.

### Running Notes

- The BenchmarkConsoleApp project can only run in Release mode.
- Which dataset is being benchmarked is chosen through commenting and uncommenting the appropriate lines in the constructor of the OppositeHomozygoteBenchmarks class.
