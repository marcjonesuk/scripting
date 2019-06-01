``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14 (18A391) [Darwin 18.0.0]
Intel Core i7-4770HQ CPU 2.20GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100-preview4-011223
  [Host] : .NET Core 3.0.0-preview4-27615-11 (CoreCLR 4.6.27615.73, CoreFX 4.700.19.21213), 64bit RyuJIT
  Core   : .NET Core 3.0.0-preview4-27615-11 (CoreCLR 4.6.27615.73, CoreFX 4.700.19.21213), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|      Method |     Mean |    Error |   StdDev | Rank |
|------------ |---------:|---------:|---------:|-----:|
| IfStatement | 168.6 ns | 3.687 ns | 4.663 ns |    1 |
