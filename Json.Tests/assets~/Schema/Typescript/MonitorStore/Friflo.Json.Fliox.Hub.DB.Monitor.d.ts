// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbCommands }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostDetails }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { RequestCount } from "./Friflo.Json.Fliox.Hub.Host.Stats";
import { int32 }        from "./Standard";
import { Change }       from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";

// schema documentation only - not implemented right now
export interface MonitorStore {
    // --- containers
    hosts      : { [key: string]: HostHits };
    users      : { [key: string]: UserHits };
    clients    : { [key: string]: ClientHits };
    histories  : { [key: string]: HistoryHits };

    // --- commands
    ["ClearStats"]         (param: ClearStats) : ClearStatsResult;
    ["std.Echo"]           (param: any) : any;
    ["std.Containers"]     (param: any) : DbContainers;
    ["std.Commands"]       (param: any) : DbCommands;
    ["std.Schema"]         (param: any) : DbSchema;
    ["std.Stats"]          (param: any) : DbStats;
    ["std.Details"]        (param: any) : HostDetails;
    ["std.Cluster"]        (param: any) : HostCluster;
}

export class HostHits {
    id      : string;
    counts  : RequestCount;
}

export class UserHits {
    id       : string;
    clients  : string[];
    counts?  : RequestCount[] | null;
}

export class ClientHits {
    id      : string;
    user    : string;
    counts? : RequestCount[] | null;
    event?  : EventDelivery | null;
}

export class HistoryHits {
    id          : int32;
    counters    : int32[];
    lastUpdate  : int32;
}

export class EventDelivery {
    seq          : int32;
    queued       : int32;
    messageSubs? : string[] | null;
    changeSubs?  : ChangeSubscriptions[] | null;
}

export class ChangeSubscriptions {
    container  : string;
    changes    : Change[];
    filter?    : string | null;
}

export class ClearStats {
}

export class ClearStatsResult {
}

