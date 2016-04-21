﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ductus.FluentDocker.Executors;
using Ductus.FluentDocker.Executors.Parsers;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Model.Machines;
using Ductus.FluentDocker.Services;

namespace Ductus.FluentDocker.Commands
{
  public static class Machine
  {
    public static CommandResponse<IList<MachineLsResponse>> Ls()
    {
      return new ProcessExecutor<MachineLsResponseParser, IList<MachineLsResponse>>(
        "docker-machine".DockerPath(), "ls --format=\"{{.Name}};{{.State}};{{.URL}}\"").Execute();
    }

    public static CommandResponse<MachineConfiguration> Inspect(this string machine)
    {
      return
        new ProcessExecutor<MachineInspectResponseParser, MachineConfiguration>(
          "docker-machine".DockerPath(), $"inspect {machine}").Execute();
    }

    public static CommandResponse<string> Start(this string machine)
    {
      return
        new ProcessExecutor<MachineStartStopResponseParser, string>(
          "docker-machine".DockerPath(), $"start {machine}").Execute();
    }

    public static CommandResponse<string> Stop(this string machine)
    {
      return
        new ProcessExecutor<MachineStartStopResponseParser, string>(
          "docker-machine".DockerPath(), $"stop {machine}").Execute();
    }

    public static CommandResponse<IDictionary<string, string>> Environment(this string machine)
    {
      return
        new ProcessExecutor<MachineEnvResponseParser, IDictionary<string, string>>(
          "docker-machine".DockerPath(), $"env {machine}").Execute();
    }

    /// <summary>
    ///   Creates a machine by passing the raw options (see docker-machine --help).
    /// </summary>
    /// <param name="machine">The name of the machine</param>
    /// <param name="driver">The machine driver</param>
    /// <param name="options">The "raw" docker-machine options.</param>
    /// <returns>Creation log.</returns>
    public static CommandResponse<string> Create(this string machine, string driver, params string[] options)
    {
      var opts = options.Aggregate(string.Empty, (current, option) => current + $"{option} ");
      var args = string.IsNullOrEmpty(opts) ? $"create -d {driver} {machine}" : $"create -d {driver} {opts} {machine}";

      return
        new ProcessExecutor<MachineCreateResponseParser, string>(
          "docker-machine".DockerPath(),
          args).Execute();
    }

    public static CommandResponse<string> Create(this string machine, int memMb, int volumeMb, int cpuCnt,
      params string[] options)
    {
      return Create(machine, "virtualbox", $"--virtualbox-memory \"{memMb}\"",
        $"--virtualbox-disk-size \"{volumeMb}\"", $"--virtualbox-cpu-count \"{cpuCnt}\"");
    }

    public static CommandResponse<string> Delete(this string machine, bool force)
    {
      var args = "rm -y " + (force ? "-f " : string.Empty) + machine;
      return
        new ProcessExecutor<MachineRmResponseParser, string>(
          "docker-machine".DockerPath(),
          args).Execute();
    }

    public static Uri Uri(this string machine)
    {
      var resp =
        new ProcessExecutor<SingleStringResponseParser, string>(
          "docker-machine".DockerPath(), $"url {machine}").Execute();

      return resp.Data.StartsWith("Host is not running") ? null : new Uri(resp.Data);
    }

    public static ServiceRunningState Status(this string machine)
    {
      var resp = new ProcessExecutor<SingleStringResponseParser, string>(
        "docker-machine".DockerPath(), $"status {machine}").Execute();

      if (!resp.Success)
      {
        return ServiceRunningState.Unknown;
      }

      switch (resp.Data)
      {
        case "Stopped":
          return ServiceRunningState.Stopped;
        case "Running":
          return ServiceRunningState.Running;
        default:
          return ServiceRunningState.Unknown;
      }
    }
  }
}