﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Fliox.Engine.ECS;
using Friflo.Json.Fliox.Hub.Client;


namespace Friflo.Fliox.Engine.Client;


public class GameEntityStoreSync : IGameEntityStoreSync
{

    private readonly LocalEntities<long, DataNode>  dataNodes;
    
    public GameEntityStoreSync(SceneClient client) {
        dataNodes = client.nodes.Local;
    }
        
    public bool TryGetDataNode(long pid, out DataNode dataNode) {
        return dataNodes.TryGetEntity(pid, out dataNode);
    }

    public void AddDataNode(DataNode dataNode) {
        dataNodes.Add(dataNode);
    }
}