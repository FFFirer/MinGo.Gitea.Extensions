using MinGo.Gitea.Application.Services;
using Xunit;

namespace MinGo.Gitea.Core.Tests;

public class WorkflowDispatchInputsParsingTests
{
    [Fact]
    public void ParsesNestedMappingForm()
    {
        var yaml = """
            name: Build & Push Docker Image

            on:
              workflow_dispatch:
                inputs:
                  tag:
                    description: "主仓库 git tag，如 v1.0.0"
                    required: true
                    type: string

            jobs:
              build:
                runs-on: ubuntu-latest
            """;

        var inputs = WorkflowService.ParseWorkflowDispatchInputs(yaml);

        var input = Assert.Single(inputs);
        Assert.Equal("tag", input.Name);
        Assert.Equal("string", input.Type);
        Assert.True(input.Required);
        Assert.Equal("主仓库 git tag，如 v1.0.0", input.Description);
    }

    [Fact]
    public void ParsesListFormOfOnKey()
    {
        var yaml = """
            on:
              - push
              - workflow_dispatch:
                  inputs:
                    env:
                      required: false
                      default: staging
            """;

        var inputs = WorkflowService.ParseWorkflowDispatchInputs(yaml);

        var input = Assert.Single(inputs);
        Assert.Equal("env", input.Name);
        Assert.Equal("staging", input.DefaultValue);
        Assert.False(input.Required);
    }

    [Fact]
    public void ParsesChoiceOptionsAndSortsRequiredFirst()
    {
        var yaml = """
            on:
              workflow_dispatch:
                inputs:
                  zebra:
                    type: string
                  level:
                    type: choice
                    required: true
                    options:
                      - low
                      - high
            """;

        var inputs = WorkflowService.ParseWorkflowDispatchInputs(yaml);

        Assert.Equal(["level", "zebra"], inputs.Select(i => i.Name));
        Assert.Equal(["low", "high"], inputs[0].Options);
    }

    [Fact]
    public void ReturnsEmptyForDispatchWithoutInputsOrMissingOn()
    {
        Assert.Empty(WorkflowService.ParseWorkflowDispatchInputs("on:\n  workflow_dispatch:\n"));
        Assert.Empty(WorkflowService.ParseWorkflowDispatchInputs("on: push\n"));
        Assert.Empty(WorkflowService.ParseWorkflowDispatchInputs("jobs:\n  build:\n"));
        Assert.Empty(WorkflowService.ParseWorkflowDispatchInputs(""));
        Assert.Empty(WorkflowService.ParseWorkflowDispatchInputs(null));
    }

    [Fact]
    public void ReturnsEmptyForInvalidYaml()
    {
        Assert.Empty(WorkflowService.ParseWorkflowDispatchInputs("on:\n  - [\nbroken: : yaml"));
    }
}
