// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int32 }              from "./Standard";
import { DbContainers }       from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbMessages }         from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }           from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }            from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { TransactionResult }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { TransactionEnd }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostParam }          from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostInfo }           from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }        from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserParam }          from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserResult }         from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { ClientParam }        from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { ClientResult }       from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { RequestCount }       from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { SubscriptionEvents } from "./Friflo.Json.Fliox.Hub.DB.Cluster";

/**
 * **MonitorStore** expose access information of the Hub and its databases:  
 * - request and task count executed per user   
 * - request and task count executed per client. A user can access without, one or multiple client ids.   
 * - events sent to (or buffered for) clients subscribed by these clients.   
 * - aggregated access counts of the Hub in the last 30 seconds and 30 minutes.
 */
// schema documentation only - not implemented right now
export interface MonitorStore {
    // --- containers
    hosts      : { [key: string]: HostHits };
    users      : { [key: string]: UserHits };
    clients    : { [key: string]: ClientHits };
    histories  : { [key: string]: HistoryHits };

    // --- commands
    /** Reset all request, task and event counters */
    ["ClearStats"]               (param: ClearStats | null) : ClearStatsResult;
    /** Echos the given parameter to assure the database is working appropriately. */
    ["std.Echo"]                 (param: any) : any;
    /** A command that completes after a specified number of milliseconds. */
    ["std.Delay"]                (param: int32) : int32;
    /** List all database containers */
    ["std.Containers"]           () : DbContainers;
    /** List all database commands and messages */
    ["std.Messages"]             () : DbMessages;
    /** Return the Schema assigned to the database */
    ["std.Schema"]               () : DbSchema;
    /** Return the number of entities of all containers (or the given container) of the database */
    ["std.Stats"]                (param: string | null) : DbStats;
    /** Starts a transaction containing all subsequent **SyncTask**'s */
    ["std.TransactionBegin"]     () : TransactionResult;
    /** Ends a transaction started previously with **TransactionBegin** */
    ["std.TransactionEnd"]       (param: TransactionEnd | null) : TransactionResult;
    /** Returns general information about the Hub like version, host, project and environment name */
    ["std.Host"]                 (param: HostParam | null) : HostInfo;
    /** List all databases and their containers hosted by the Hub */
    ["std.Cluster"]              () : HostCluster;
    /** Return the groups of the current user. Optionally change the groups of the current user */
    ["std.User"]                 (param: UserParam | null) : UserResult;
    /** Return client specific infos and adjust general client behavior like **queueEvents** */
    ["std.Client"]               (param: ClientParam | null) : ClientResult;
}

/** number of requests and tasks executed by the host. Container contains always a single record */
export class HostHits {
    /** host name */
    id      : string;
    /** number of executed requests and tasks per database */
    counts  : RequestCount;
}

/** all user clients and number of executed user requests and tasks */
export class UserHits {
    /** user id */
    id       : string;
    /** list of clients owned by a user */
    clients  : string[];
    /** number executed requests and tasks per database */
    counts?  : RequestCount[] | null;
}

/** information about requests, tasks, events and subscriptions of a client */
export class ClientHits {
    /** client id */
    id                  : string;
    /** user owning the client */
    user                : string;
    /** number executed requests and tasks per database */
    counts?             : RequestCount[] | null;
    /** number of sent or queued client events and its message and change subscriptions */
    subscriptionEvents? : SubscriptionEvents | null;
}

/** aggregated counts of latest requests. Each record uses a specific aggregation interval. */
export class HistoryHits {
    /** time in seconds for an aggregation interval */
    id          : int32;
    /** number of requests executed in each interval */
    counters    : int32[];
    /** last update of the **HistoryHits** record */
    lastUpdate  : int32;
}

export class ClearStats {
}

export class ClearStatsResult {
}

