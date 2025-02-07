using BenchmarkDotNet.Attributes;
using OpenCL.Net;

[MemoryDiagnoser, RankColumn]
public class CpuGpuBench
{
    private const string KernelSource = @"
    __kernel void square(__global float* input, __global float* output) {
        int i = get_global_id(0);
        output[i] = input[i] * input[i];
    }";

    public float[] inputArray;

    [GlobalSetup]
    public void Setup()
    {
        inputArray = Enumerable.Repeat(1, 256000).Select(x => (float)Random.Shared.NextDouble()).ToArray();
    }

    [Benchmark]
    public float[] CpuSingleThreadSquare()
    {
        var outputArray = new float[inputArray.Length];
        for (var index = 0; index < inputArray.Length; index++)
            outputArray[index] = Pow(inputArray[index], 2);

        return outputArray;
    }

    [Benchmark]
    public float[] CpuParallelSquare()
    {
        var outputArray = new float[inputArray.Length];
        Parallel.For(0, inputArray.Length, (index) =>
        {
            outputArray[index] = Pow(inputArray[index], 2);
        });
        return outputArray;
    }

    private static float Pow(float x, int n)
    {
        float result = 1;
        var positive = n >= 0;
        long pow = n < 0 ? -1L * n : n;
        while (pow > 0)
        {
            if (pow % 2 == 1)
                result = result * x;
            x = x * x;
            pow >>= 1;
        }

        if (positive)
            return result;

        return 1 / result;
    }


    [Benchmark]
    public float[] GpuSquare()
    {
        int length = inputArray.Length;
        float[] outputArray = new float[length];

        // Получаем платформу OpenCL
        ErrorCode error;
        Platform platform = Cl.GetPlatformIDs(out error).First();
        Device device = Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error).First();
        Context context = Cl.CreateContext(null, 1, new[] { device }, null, IntPtr.Zero, out error);
        CommandQueue commandQueue = Cl.CreateCommandQueue(context, device, CommandQueueProperties.None, out error);

        // Создаем буферы
        IMem<float> inputBuffer = Cl.CreateBuffer<float>(context, MemFlags.ReadOnly | MemFlags.CopyHostPtr, inputArray, out error);
        IMem<float> outputBuffer = Cl.CreateBuffer<float>(context, MemFlags.WriteOnly, length, out error);

        // Создаем и компилируем программу
        var program = Cl.CreateProgramWithSource(context, 1, new[] { KernelSource }, null, out error);
        Cl.BuildProgram(program, 1, new[] { device }, string.Empty, null, IntPtr.Zero);
        Kernel kernel = Cl.CreateKernel(program, "square", out error);

        // Привязываем аргументы
        Cl.SetKernelArg(kernel, 0, inputBuffer);
        Cl.SetKernelArg(kernel, 1, outputBuffer);

        // Запускаем выполнение
        Event kernelEvent;
        Cl.EnqueueNDRangeKernel(commandQueue, kernel, 1, null, new[] { (IntPtr)length }, null, 0, null, out kernelEvent);

        // Читаем результаты
        Cl.EnqueueReadBuffer(commandQueue, outputBuffer, Bool.True, IntPtr.Zero, length * sizeof(float), outputArray, 0, null, out kernelEvent);

        // Освобождаем ресурсы
        Cl.ReleaseKernel(kernel);
        Cl.ReleaseProgram(program);
        Cl.ReleaseMemObject(inputBuffer);
        Cl.ReleaseMemObject(outputBuffer);
        Cl.ReleaseCommandQueue(commandQueue);
        Cl.ReleaseContext(context);

        return outputArray;
    }
}