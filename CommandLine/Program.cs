﻿using System.Diagnostics.CodeAnalysis;
using CommandLine;
using McMaster.Extensions.CommandLineUtils;
using NLog;
using Z2Randomizer.Core;

namespace Z2Randomizer.CommandLine;

public class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Program))]
    public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);

    [Option(ShortName = "f", Description = "Flag string")]
    public string? Flags { get; }

    [Option(ShortName = "r", Description = "Path to the base ROM file")]
    public string? Rom { get; }

    [Option(ShortName = "s", Description = "[Optional] Seed used to generate the shuffled ROM")]
    public int? Seed { get; set; }

    [Option(ShortName = "po", Description = "[Optional] Specifies a player options file to use for misc settings")]
    public string? PlayerOptions { get; }

    private RandomizerConfiguration? configuration;

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Program))]
    private int OnExecute()
    {
        SetNlogLogLevel(LogLevel.Info);
        if (string.IsNullOrEmpty(Flags))
        {
            logger.Error("The flag string is required");
            return -1;
        }

        this.configuration = new RandomizerConfiguration(Flags);

        if (!Seed.HasValue) 
        {
            var r = new Random();
            this.Seed = r.Next(1000000000);
        } 
        this.configuration.Seed = Seed.Value;

        if (Rom == null || Rom == string.Empty)
        {
            logger.Error("The ROM path is required");
            return -2;
        } 
        else if (!File.Exists(Rom))
        {
            logger.Error($"The specified ROM file does not exist: {Rom}");
            return -3;
        }

        configuration.FileName = Rom;

        logger.Info($"Flags: {Flags}");
        logger.Info($"Rom: {Rom}");
        logger.Info($"Seed: {Seed}");

        try
        {
            var playerOptionsService = new PlayerOptionsService();
            var playerOptions = playerOptionsService.LoadFromFile(this.PlayerOptions);
            if (playerOptions == null)
            {
                throw new Exception("Could not load player options");
            }

            playerOptionsService.ApplyOptionsToConfiguration(playerOptions, configuration);
        }
        catch (Exception exception)
        {
            logger.Fatal(exception);
            return -4;
        }

        Randomize().Wait();

        return 0;
    }

    public async Task Randomize()
    {
        // Exception? generationException = null;
        // var worker = new BackgroundWorker();
        // worker.DoWork += new DoWorkEventHandler(RandomizationWorker!);
        // worker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorker1_ProgressChanged!);
        // worker.WorkerReportsProgress = true;
        // worker.WorkerSupportsCancellation = true;
        // worker.RunWorkerCompleted += (completed_sender, completed_event) =>
        // {
        //     generationException = completed_event.Error;
        // };
        // worker.RunWorkerAsync();
        var cts = new CancellationTokenSource();
        var engine = new DesktopJsEngine();
        var randomizer = new Hyrule(configuration!, engine);
        var rom = await randomizer.Randomize((str) => { logger.Info(str); }, cts.Token);

        if (rom != null)
        {
            char os_sep = Path.DirectorySeparatorChar;
            var filename = configuration!.FileName;
            string newFileName = filename.Substring(0, filename.LastIndexOf(os_sep) + 1) + "Z2_" + Seed + "_" + Flags + ".nes";
            File.WriteAllBytes(newFileName, rom);
            logger.Info("File " + "Z2_" + this.Seed + "_" + this.Flags + ".nes" + " has been created!");
        }
        else
        {
            logger.Error("An exception occurred generating the rom");
        }
    }

    private static void SetNlogLogLevel(LogLevel level)
    {
        // Uncomment these to enable NLog logging. NLog exceptions are swallowed by default.
        ////NLog.Common.InternalLogger.LogFile = @"C:\Temp\nlog.debug.log";
        ////NLog.Common.InternalLogger.LogLevel = LogLevel.Debug;

        if (level == LogLevel.Off)
        {
            LogManager.DisableLogging();
        }
        else
        {
            if (!LogManager.IsLoggingEnabled())
            {
                LogManager.EnableLogging();
            }

            foreach (var rule in LogManager.Configuration.LoggingRules)
            {
                // Iterate over all levels up to and including the target, (re)enabling them.
                for (int i = level.Ordinal; i <= 5; i++)
                {
                    rule.EnableLoggingForLevel(LogLevel.FromOrdinal(i));
                }
            }
        }

        LogManager.ReconfigExistingLoggers();
    }
}