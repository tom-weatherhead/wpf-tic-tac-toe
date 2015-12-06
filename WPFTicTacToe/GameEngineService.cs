﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.5456
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------



[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(ConfigurationName="IGameEngineService")]
public interface IGameEngineService
{
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGameEngineService/FindBestMove", ReplyAction="http://tempuri.org/IGameEngineService/FindBestMoveResponse")]
    int FindBestMove(int boardDimension, string boardAsString, bool playerIsX, int ply);
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public interface IGameEngineServiceChannel : IGameEngineService, System.ServiceModel.IClientChannel
{
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public partial class GameEngineServiceClient : System.ServiceModel.ClientBase<IGameEngineService>, IGameEngineService
{
    
    public GameEngineServiceClient()
    {
    }
    
    public GameEngineServiceClient(string endpointConfigurationName) : 
            base(endpointConfigurationName)
    {
    }
    
    public GameEngineServiceClient(string endpointConfigurationName, string remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public GameEngineServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public GameEngineServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(binding, remoteAddress)
    {
    }
    
    public int FindBestMove(int boardDimension, string boardAsString, bool playerIsX, int ply)
    {
        return base.Channel.FindBestMove(boardDimension, boardAsString, playerIsX, ply);
    }
}