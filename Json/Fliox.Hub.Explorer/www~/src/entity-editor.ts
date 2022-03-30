import { el, createEl, Resource, Entity, parseAst, MessageCategory }   from "./types.js";
import { App, app }                                                 from "./index.js";
import { Schema }                                                   from "./schema.js";

import { MessageType, JsonType, FieldType } from "../../../../Json.Tests/assets~/Schema/Typescript/JSONSchema/Friflo.Json.Fliox.Schema.JSON";
import { DbContainers, DbMessages }         from "../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";

type AddRelation = (value: jsonToAst.ValueNode, container: string) => void;

type FindRange = {
    readonly entity:        monaco.Range | null;
    readonly value:         monaco.Range | null;
    readonly lastProperty:  monaco.Range | null;
    readonly lastPath:      string[];
}

type ExplorerEditor = "command" | "entity" | "dbInfo"
type CodeEditor     = monaco.editor.IStandaloneCodeEditor;


const entityExplorer    = el("entityExplorer");
const writeResult       = el("writeResult");
const readEntitiesDB    = el("readEntitiesDB");
const readEntities      = el("readEntities");
const readEntitiesCount = el("readEntitiesCount");
const catalogSchema     = el("catalogSchema");
const explorerTools     = el("explorerTools");
const entityType        = el("entityType");
const entityIdsContainer= el("entityIdsContainer");
const entityIdsCount    = el("entityIdsCount");
const entityIdsGET      = el("entityIdsGET")    as HTMLAnchorElement;
const entityIdsInput    = el("entityIdsInput")  as HTMLInputElement;
const entityIdsReload   = el("entityIdsReload");

const entityFilter      = el("entityFilter")    as HTMLInputElement;
const filterRow         = el("filterRow");
const commandSignature  = el("commandSignature");
const commandAnchor     = el("commandAnchor")   as HTMLAnchorElement;
const commandDocs       = el("commandDocs");

// entity/command editor
const commandValueContainer  = el("commandValueContainer");
const commandParamBar        = el("commandParamBar");
const entityContainer        = el("entityContainer");


type MsgType = "command" | "message";

interface Message {
    li:     HTMLLIElement,
    type:   MsgType;
}

type MessageMap = {
    [key: string] : Message
};

// ----------------------------------------------- EntityEditor -----------------------------------------------
export class EntityEditor
{
    private entityEditor:       CodeEditor  = null;
    private commandValueEditor: CodeEditor  = null;
    private selectedCommandEl:  HTMLElement = null;
    private selectedCommands:   { [database: string]: string} = { }; // store selected command per database


    public initEditor(entityEditor: CodeEditor, commandValueEditor: CodeEditor) : void {
        this.entityEditor       = entityEditor;
        this.commandValueEditor = commandValueEditor;
    }

    private setEditorHeader(show: "entity" | "command" | "database" | "none") {
        const displayEntity  = show == "entity"     ? "contents" : "none";
        const displayCommand = show == "command"    ? "contents" : "none";
        const displayDB      = show == "database"   ? "contents" : "none";
        el("entityTools")  .style.display = displayEntity;        
        el("entityHeader") .style.display = displayEntity;        
        el("commandTools") .style.display = displayCommand;
        el("commandHeader").style.display = displayCommand;
        el("databaseTools").style.display = displayDB;
    }

    public async sendCommand() : Promise<void> {
        const param     = this.commandValueEditor.getValue();
        const e         = this.entityIdentity;
        const database  = e.database;
        const command   = e.command;
        const type      = e.msgType;

        const response  = await App.restRequest("POST", param, database, null, null, `${type}=${command}`);
        let content     = await response.text();

        content         = app.formatJson(app.config.formatResponses, content);
        this.entityEditor.setValue(content);
    }

    private setDatabaseInfo(database: string, dbContainer: DbContainers) {
        const schemaType                    = app.getSchemaType(database);
        const oasLink                       = App.getOpenApiLink(database, "open database API", "");
        el("databaseName").innerHTML        = App.getDatabaseLink(database);
        el("databaseSchema").innerHTML      = `${schemaType} ${oasLink}`;
        el("databaseTypes").innerHTML       = app.getSchemaTypes(database);
        el("databaseStorage").innerHTML     = dbContainer.storage;
        el("schemaDescription").innerHTML   = app.getSchemaDescription(database);
    }

    private setExplorerSelection(database: string, command: string | null, element: HTMLElement) {
        this.selectedCommandEl?.classList.remove("selected");
        this.selectedCommandEl  = element;
        element.classList.add("selected");
        this.selectedCommands[database] = command;
    }

    private selectDatabaseInfo(database: string) {
        this.setExplorerEditor("dbInfo");
        const infoEl = el("databaseInfo");
        this.setExplorerSelection(database, null, infoEl);
        this.setEditorHeader("database");
    }

    private selectCommand(database: string, command: string, message: Message) {
        this.setEditorHeader("command");
        this.setExplorerSelection(database, command, message.li);
        this.showCommand(database, command, message.type);
    }

    public listCommands (database: string, dbMessages: DbMessages, dbContainer: DbContainers) : void {
        app.explorer.initExplorer(null, null, null, null);
        this.setDatabaseInfo(database, dbContainer);

        const schemaType                = app.getSchemaType(database);
        catalogSchema.innerHTML         = schemaType;
        explorerTools.innerHTML         = "";
        el("databaseLabel").innerHTML   = schemaType;
        filterRow.style.visibility      = "hidden";
        entityFilter.style.visibility   = "hidden";
        const messagesLink              = App.getMessagesLink(database);
        readEntitiesDB.innerHTML        = messagesLink;
        readEntities.innerHTML          = "";
        readEntitiesCount.innerHTML     = "";

        const ulDatabase            = createEl('ul');
        ulDatabase.classList.value  = "database";

        // database link
        const databaseLink          = createEl('li');
        databaseLink.id             = "databaseInfo";
        databaseLink.style.display  = "flex";
        databaseLink.style.marginBottom = "5px";
        const databaseAnchor        = createEl("a");
        databaseAnchor.href         = "#";
        databaseAnchor.style.width  = "100%";
        databaseAnchor.target       = "blank";
        databaseAnchor.rel          = "noopener noreferrer";
        databaseAnchor.onclick      = (ev) => { this.selectDatabaseInfo(database); ev.preventDefault(); };
        databaseAnchor.innerHTML    = '<span style="" title="show general database information">database info</span>';
        databaseLink.append(databaseAnchor);
        ulDatabase.append(databaseLink);

        const messageMap: MessageMap = {};
        // commands link
        const commandLink   = this.createMessagesDocLink(database, "commands");
        ulDatabase.append(commandLink);
        // commands list
        const messagesLi    = this.createMessagesLi(database, "command", dbMessages.commands, messageMap);
        ulDatabase.appendChild(messagesLi);

        const messages = dbMessages.messages;
        if (messages && messages.length > 0) {
            // messages link
            const commandLink   = this.createMessagesDocLink(database, "messages");
            ulDatabase.append(commandLink);
            // messages list
            const messagesLi    = this.createMessagesLi(database, "message", dbMessages.messages, messageMap);
            ulDatabase.appendChild(messagesLi);
        }
        entityExplorer.innerText = "";
        entityExplorer.appendChild(ulDatabase);
        
        const selectedCommand   = this.selectedCommands[database];
        const message           = messageMap[selectedCommand];
        if (message) {
            this.selectCommand(database, selectedCommand, message);
            message.li.scrollIntoView();
        } else {
            this.selectDatabaseInfo(database);
        }        
    }

    private createMessagesDocLink (database: string, category: MessageCategory) : HTMLLIElement {
        const categoryEl        = createEl('li');
        const oasLink           = category == "commands" ? " " + App.getOpenApiLink(database, "open commands API", "#/commands") : "";
        categoryEl.innerHTML    = app.getSchemaCommand(database, category, category) + oasLink;
        if (!app.databaseSchemas[database]) {
            categoryEl.innerHTML    = `<span style="opacity: 0.5;">${category}</span>`;
        }
        const child             = categoryEl.firstElementChild as HTMLElement;
        child.style.marginLeft  = "10px";
        return categoryEl;
    }

    private createMessagesLi(database: string, type: MsgType, messages: string[], messageMap: MessageMap) : HTMLLIElement {
        const ulCommands    = createEl('ul');
        ulCommands.onclick  = (ev) => {
            const path      = ev.composedPath() as HTMLElement[];
            let selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent

            const tagName = selectedElement.tagName;
            if (tagName == "SPAN" || tagName == "DIV") {
                selectedElement = path[1];
            }
            const commandName = selectedElement.children[0].textContent;
            const message       = messageMap[commandName];
            this.selectCommand(database, commandName, message);

            if (path[0].classList.contains("command")) {
                this.sendCommand();
            }
        };
        for (const message of messages) {
            const liCommand             = createEl('li');
            const commandLabel          = createEl('div');
            commandLabel.innerText      = message;
            liCommand.appendChild(commandLabel);
            const runCommand            = createEl('div');
            runCommand.classList.value  = "command";
            runCommand.title            = `Send ${type} using POST`;
            liCommand.appendChild(runCommand);

            ulCommands.append(liCommand);
            messageMap[message] = { li: liCommand, type: type };
        }
        const liCommands  = createEl('li');
        liCommands.append(ulCommands);
        return liCommands;
    }

    private entityIdentity = { } as {
        readonly    database:   string,
        readonly    container:  string,
                    entityIds:  string[],
        readonly    command?:   string,
        readonly    msgType?:   MsgType
    }

    public  entityHistoryPos    = -1;
    private entityHistory: {
        selection?: monaco.Selection,
        route:      Resource
    }[] = [];

    private storeCursor() {
        if (this.entityHistoryPos < 0)
            return;
        this.entityHistory[this.entityHistoryPos].selection    = this.entityEditor.getSelection();
    }

    public navigateEntity(pos: number) : void {
        if (pos < 0 || pos >= this.entityHistory.length)
            return;
        this.storeCursor();
        this.entityHistoryPos   = pos;
        const entry             = this.entityHistory[pos];
        this.loadEntities(entry.route, true, entry.selection);
    }

    public async loadEntities (p: Resource, preserveHistory: boolean, selection: monaco.IRange) : Promise<string> {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");
        entityType.innerHTML    = app.getEntityType  (p.database, p.container);
        writeResult.innerHTML   = "";
        this.setEntitiesIds(p.database, p.container, p.ids);
        if (p.ids.length == 0) {
            this.setEntityValue(p.database, p.container, "");
            return null;
        }
        // entityIdsEl.innerHTML   = `${entityLink}<span class="spinner"></span>`;

        if (!preserveHistory) {
            this.storeCursor();
            this.entityHistory[++this.entityHistoryPos] = { route: {...p} };
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
        this.entityIdentity = {
            database:   p.database,
            container:  p.container,
            entityIds:  [...p.ids]
        };
        // execute GET request
        const ids       = p.ids.length == 1 ? p.ids[0] : p.ids; // load as object if exact one id
        const response  = await EntityEditor.requestIds(p.database, p.container, ids);
        let content     = await response.text();

        content         = app.formatJson(app.config.formatEntities, content);
        this.setEntitiesIds(p.database, p.container, p.ids);
        if (!response.ok) {
            this.setEntityValue(p.database, p.container, content);
            return null;
        }
        // console.log(entityJson);
        this.setEntityValue(p.database, p.container, content);
        if (selection)  this.entityEditor.setSelection(selection);        
        // this.entityEditor.focus(); // not useful - annoying: open soft keyboard on phone
        return content;
    }

    private static async requestIds(database: string, container: string, requestIds: string | string[]) : Promise<Response> {
        const idsStr = JSON.stringify(requestIds);
        // Typical limit for urls in Chrome, ASP.NET: 2048
        if (idsStr.length > 2000) {
            return await App.restRequest("POST", idsStr, database, `${container}/bulk-get`, null, null);
        }
        return await App.restRequest("GET", null, database, container, requestIds, null);
    }

    private updateGetEntitiesAnchor(database: string, container: string) {
        // console.log("updateGetEntitiesAnchor");
        const idsStr = entityIdsInput.value;
        const ids    = idsStr.split(",");
        let   len    = ids.length;
        if (len == 1 && ids[0] == "") len = 0;
        entityIdsContainer.onclick      = () => app.explorer.loadContainer({ database: database, container: container, ids: null }, null);
        entityIdsContainer.innerText    = `« ${container}`;
        entityIdsCount.innerText        = len > 0 ? `(${len})` : "";

        let getUrl: string;        
        if (len == 1) {
            getUrl = `./rest/${database}/${container}/${ids[0]}`;
        } else {
            getUrl = `./rest/${database}/${container}?ids=${idsStr}`;
        }
        entityIdsGET.href       = getUrl;
    }

    private setEntitiesIds (database: string, container: string, ids: string[]) {
        entityIdsReload.onclick     = () => this.loadInputEntityIds      (database, container);
        entityIdsInput.onchange     = () => this.updateGetEntitiesAnchor (database, container);
        entityIdsInput.onkeydown    = e => this.onEntityIdsKeyDown   (e, database, container);
        entityIdsInput.value        = ids.join (",");
        this.updateGetEntitiesAnchor(database, container);
    }

    public static formatResult (action: string, statusCode: number, status: string, message: string) : string {
        const color = 200 <= statusCode && statusCode < 300 ? "green" : "red";
        return `<span>
            <span style="opacity:0.7">${action} status:</span>
            <span style="color: ${color};">${statusCode} ${status}</span>
            <span>${message}</span>
        </span>`;
    }

    private async loadInputEntityIds (database: string, container: string) {
        const ids               = entityIdsInput.value == "" ? [] : entityIdsInput.value.split(",");
        const unchangedSelection= EntityEditor.arraysEquals(this.entityIdentity.entityIds, ids);
        const p: Resource       = { database, container, ids };
        const response          = await this.loadEntities(p, true, null);
        if (unchangedSelection)
            return;
        let   json              = JSON.parse(response) as Entity[];
        if (json == null) {
            json = [];
        } else {
            if (!Array.isArray(json))
                json = [json];
        }
        const type          = app.getContainerSchema(database, container);
        app.explorer.updateExplorerEntities(json, type);
        this.selectEntities(database, container, ids);
    }

    private onEntityIdsKeyDown(event: KeyboardEvent, database: string, container: string) {
        if (event.code != 'Enter')
            return;
        this.loadInputEntityIds(database, container);
    }

    public clearEntity (database: string, container: string) : void {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");

        this.entityIdentity = {
            database:   database,
            container:  container,
            entityIds:  [],
            command:    null,
            msgType:    null
        };
        entityType.innerHTML    = app.getEntityType (database, container);
        writeResult.innerHTML   = "";
        this.setEntitiesIds(database, container, []);
        this.setEntityValue(database, container, "");
    }

    public  static getEntityKeyName (entityType: JsonType) : string {
        if (entityType?.key)
            return entityType.key;
        return "id";
    }

    public async saveEntitiesAction () : Promise<void> {
        const ei        = this.entityIdentity;
        const jsonValue = this.entityModel.getValue();
        await this.saveEntities(ei.database, ei.container, jsonValue);
    }

    private async saveEntities (database: string, container: string, jsonValue: string)
    {
        let value:      Entity | Entity[];
        try {
            value = JSON.parse(jsonValue) as Entity | Entity[];
        } catch (error) {
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        const entities          = Array.isArray(value) ? value : [value];
        const type              = app.getContainerSchema(database, container);
        const keyName           = EntityEditor.getEntityKeyName(type as JsonType);
        const ids               = entities.map(entity => String(entity[keyName]));
        writeResult.innerHTML   = 'save <span class="spinner"></span>';
        const requestIds        = Array.isArray(value) ? null : ids[0];
        const response          = await App.restRequest("PUT", jsonValue, database, container, requestIds, null);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = EntityEditor.formatResult("Save", response.status, response.statusText, error);
            return;
        }
        writeResult.innerHTML = EntityEditor.formatResult("Save", response.status, response.statusText, "");
        // add or update explorer entities
        app.explorer.updateExplorerEntities(entities, type);
        if (EntityEditor.arraysEquals(this.entityIdentity.entityIds, ids))
            return;
        this.selectEntities(database, container, ids);        
    }

    private selectEntities(database: string, container: string, ids: string[]) {
        this.entityIdentity.entityIds = ids;
        this.setEntitiesIds(database, container, ids);
        const rowIndices = app.explorer.findRowIndices(ids);

        app.explorer.setSelectedEntities(ids);
        const firstRow = rowIndices[ids[0]];
        if (firstRow) {
            const focusedCell   = app.explorer.getFocusedCell();
            const column        = focusedCell?.column ?? 1;
            app.explorer.setFocusCellSelectValue(firstRow, column, "smooth");
        }
        this.entityHistory[++this.entityHistoryPos] = { route: { database: database, container: container, ids:ids }};
        this.entityHistory.length = this.entityHistoryPos + 1;        
    }

    private static arraysEquals(left: string[], right: string []) : boolean {
        if (left.length != right.length)
            return false;
        for (let i = 0; i < left.length; i++) {
            if (left[i] != right[i])
                return false;
        }
        return true;
    }

    public async deleteEntitiesAction () : Promise<void> {
        const ei = this.entityIdentity;
        await this.deleteEntities (ei.database, ei.container, ei.entityIds);
    }

    public  async deleteEntities (database: string, container: string, ids: string[]) : Promise<void> {
        writeResult.innerHTML = 'delete <span class="spinner"></span>';
        const response = await EntityEditor.deleteIds(database, container, ids);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = EntityEditor.formatResult("Delete", response.status, response.statusText, error);
            return;
        }
        this.entityIdentity.entityIds = [];
        writeResult.innerHTML = EntityEditor.formatResult("Delete", response.status, response.statusText, "");
        this.setEntityValue(database, container, "");
        app.explorer.removeExplorerIds(ids);        
    }

    private static async deleteIds(database: string, container: string, ids: string[]) : Promise<Response> {
        if (ids.length == 1) {
            return await App.restRequest("DELETE", null, database, `${container}/${ids[0]}`, null, null);
        }
        const idsStr = JSON.stringify(ids);
        return await App.restRequest("POST", idsStr, database, `${container}/bulk-delete`, null, null);
}

    private entityModel:    monaco.editor.ITextModel;
    private entityModels:   {[key: string]: monaco.editor.ITextModel} = { };

    private getModel (url: string) : monaco.editor.ITextModel {
        this.entityModel = this.entityModels[url];
        if (!this.entityModel) {
            const entityUri         = monaco.Uri.parse(url);
            this.entityModel        = monaco.editor.createModel(null, "json", entityUri);
            this.entityModels[url]  = this.entityModel;
        }
        return this.entityModel;
    }

    private setEntityValue (database: string, container: string, value: string) : jsonToAst.ValueNode {
        const url   = `entity://${database}.${container}.json`;
        const model = this.getModel(url);
        model.setValue(value);
        this.entityEditor.setModel (model);
        if (value == "")
            return null;
        const ast = parseAst(value);
        if (!ast)
            return null;
        const containerSchema = app.getContainerSchema(database, container);
        if (containerSchema) {
            try {
                this.decorateJson(this.entityEditor, ast, containerSchema, database);
            } catch (error) {
                console.error("decorateJson", error);
            }
        }
        return ast;
    }

    private decorateJson(editor: CodeEditor, ast: jsonToAst.ValueNode, containerSchema: JsonType, database: string) {        
        // --- deltaDecorations() -> [ITextModel | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ITextModel.html
        const newDecorations: monaco.editor.IModelDeltaDecoration[] = [
            // { range: new monaco.Range(7, 13, 7, 22), options: { inlineClassName: 'refLinkDecoration' } }
        ];
        EntityEditor.addRelationsFromAst(ast, containerSchema, (value, container) => {
            const range         = EntityEditor.RangeFromNode(value);
            const markdownText  = `${database}/${container}  \nFollow: (ctrl + click)`;
            const hoverMessage  = [ { value: markdownText } ];
            newDecorations.push({ range: range, options: { inlineClassName: 'refLinkDecoration', hoverMessage: hoverMessage }});
        });
        editor.deltaDecorations([], newDecorations);
    }

    private static addRelationsFromAst(ast: jsonToAst.ValueNode, schema: JsonType, addRelation: AddRelation) {
        if (ast.type == "Literal")
            return;
        for (const child of ast.children) {
            switch (child.type) {
                case "Object":
                    EntityEditor.addRelationsFromAst(child, schema, addRelation);
                    break;
                case "Array":
                    break;
                case "Property": {
                    // if (child.key.value == "employees") debugger;
                    const property = schema.properties[child.key.value];
                    if (!property)
                        continue;
                    const value = child.value;

                    switch (value.type) {
                        case "Literal": {
                            const relation = property.relation;
                            if (relation && value.value !== null) {
                                addRelation (value, relation);
                            }
                            break;
                        }
                        case "Object": {
                            const resolvedDef = property._resolvedDef;
                            if (resolvedDef) {
                                EntityEditor.addRelationsFromAst(value, resolvedDef, addRelation);
                            }
                            break;
                        }
                        case "Array": {
                            const resolvedDef2 = property.items?._resolvedDef;
                            if (resolvedDef2) {
                                EntityEditor.addRelationsFromAst(value, resolvedDef2, addRelation);
                            }
                            const relation2 = property.relation;
                            if (relation2) {
                                for (const item of value.children) {
                                    if (item.type == "Literal") {
                                        addRelation(item, relation2);
                                    }
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }

    private static hasProperty(objectNode: jsonToAst.ObjectNode, keyName: string, id: string) : boolean {
        for (const property of objectNode.children) {
            if (property.key.value != keyName)
                continue;
            const value = property.value;
            if (value.type == "Literal" && value.value == id)
                return true;
        }
        return false;
    }

    private static findArrayItem(arrayNode: jsonToAst.ArrayNode, keyName: string, id: string) : jsonToAst.ObjectNode | null {
        for(const item of arrayNode.children) {
            if (item.type != "Object")
                continue;
            if (EntityEditor.hasProperty(item, keyName, id))
                return item;
        }
        return null;
    }

    public static findPathRange(ast: jsonToAst.ValueNode, pathString: string, keyName: string, id: string) : FindRange {
        const astRange  = EntityEditor.RangeFromNode(ast);
        const path      = pathString.split('.');
        let   node      = ast;
        switch (node.type) {
            case "Array":
                node = EntityEditor.findArrayItem(node, keyName, id);
                if (!node)
                    return { entity: null, value: null, lastProperty: null, lastPath: null };
                break;
            case "Object":
                if (!EntityEditor.hasProperty(node, keyName, id))
                    return { entity: null, value: null, lastProperty: null, lastPath: null };
                break;
            default:
                return { entity: null, value: null, lastProperty: null, lastPath: null };
        }
        const entityRange                   = EntityEditor.RangeFromLoc(node.loc, 0);
        let   lastRange:    monaco.Range    = null;
        const lastPath:     string[]        = [];

        // walk path in object
        let objectNode: jsonToAst.ObjectNode    = node;
        let foundChild: jsonToAst.PropertyNode;
        let i = 0;
        for (; i < path.length; i++) {
            foundChild = null;
            const name = path[i];
            if (objectNode.type != "Object")
                return { entity: astRange, value: null, lastProperty: null, lastPath: null };
            const children = objectNode.children;
            for (const child of children) {
                if (child.key.value == name) {
                    foundChild  = child;
                    lastPath.push(name);
                    if (child.value.type == "Object") {
                        objectNode  = child.value;
                    }
                    break;
                }
            }
            const lastChild = children[children.length - 1];
            lastRange       = EntityEditor.RangeFromLoc(lastChild.loc, 0);
            if (!foundChild)
                break;
        }

        if (foundChild) {
            const valueRange = EntityEditor.RangeFromLoc(foundChild.value.loc, 0);
            return { entity: entityRange, value: valueRange, lastProperty: lastRange, lastPath };
        }
        return { entity: entityRange, value: null, lastProperty: lastRange, lastPath };
    }

    private static RangeFromNode(node: jsonToAst.ValueNode) : monaco.Range{
        const trim  = node.type == "Literal" && typeof node.value == "string" ? 1 : 0;
        return EntityEditor.RangeFromLoc(node.loc, trim);
    }

    private static RangeFromLoc(loc: jsonToAst.Location, trim: number) : monaco.Range{
        const start = loc.start;
        const end   = loc.end;
        return new monaco.Range(start.line, start.column + trim, end.line, end.column - trim);
    }

    private setCommandParam (database: string, command: string, value: string) {
        const url           = `message-param://${database}.${command}.json`;
        const isNewModel    = this.entityModels[url] == undefined;
        const model         = this.getModel(url);
        if (isNewModel) {
            model.setValue(value);
        }
        this.commandValueEditor.setModel (model);
    }

    private setCommandResult (database: string, command: string) {
        const url   = `message-result://${database}.${command}.json`;
        const model = this.getModel(url);
        this.entityEditor.setModel (model);
    }

    public commandEditWidth = "100px";
    public activeExplorerEditor: ExplorerEditor = undefined;

    public setExplorerEditor(edit : ExplorerEditor) : void {
        this.activeExplorerEditor   = edit;
        // console.log("editor:", edit);
        const commandActive         = edit == "command";
        commandValueContainer.style.display = commandActive ? "" : "none";
        commandParamBar.style.display       = commandActive ? "" : "none";
        el("explorerEdit").style.gridTemplateRows = commandActive ? `${this.commandEditWidth} var(--vbar-width) 1fr` : "0 0 1fr";

        const editorActive              = edit == "command" || edit == "entity";
        entityContainer.style.display   = editorActive      ? "" : "none";
        el("dbInfo").style.display      = edit == "dbInfo"  ? "" : "none";
        //
        app.layoutEditors();
    }

    private showCommand(database: string, command: string, type: MsgType)
    {
        this.setExplorerEditor("command");

        const schema        = app.databaseSchemas[database]?._rootSchema;
        const messages      = type == "command" ? schema?.commands : schema?.messages;
        const signature     = messages ? messages[command] : null;
        const defaultParam  = EntityEditor.getDefaultValue(signature?.param);

        this.entityIdentity = {
            database:   database,
            container:  null,
            entityIds:  null,
            command:    command,
            msgType:    type
        };
        this.setCommandParam (database, command, defaultParam); // sets command param => must be called before getCommandUrl()
        this.setCommandResult(database, command);

        commandSignature.innerHTML  = this.getCommandDocsEl(database, command, signature);
    //  commandAnchor.innerHTML     = `GET <span style="opacity:0.5">${database}?command=</span>${command}`;
        commandAnchor.innerHTML     = command; // `GET &nbsp;&nbsp;${command}`;
        commandAnchor.href          = this.getCommandUrl(database, command, type);
        commandAnchor.onfocus       = () => {            
            commandAnchor.href = this.getCommandUrl(database, command, type);
        };
        const docs                  = signature?.description;
        commandDocs.innerHTML       = docs ? docs : "";
    }

    private static getDefaultValue(fieldType: FieldType) : string {
        if (!fieldType)
            return 'null';
        const type  = Schema.getFieldType(fieldType);
        if (type.isNullable)
            return 'null';
        fieldType           = type.type;
        const resolvedDef   = fieldType._resolvedDef;
        const paramType     = resolvedDef ? resolvedDef.type : fieldType.type;
        switch (paramType) {
            case "object":  return '{}';
            case "array":   return '[]';
            case "string":  return 'null';
            case "number":  return '0';
            case "boolean": return 'false';
            default:        return 'null';
        }
    }

    private getCommandUrl(database: string, command: string, type: MsgType) {
        let param = this.commandValueEditor.getValue();
        try {
            const valueStr  = JSON.parse(param);
            param           = JSON.stringify(valueStr); // format to one line / remove white spaces
        } catch {
            // use unformatted invalid value instead
        }
        const commandParam  = param == "null" ? "" : `&param=${param}`;
        return `./rest/${database}?${type}=${command}${commandParam}`;
    }

    private getCommandDocsEl(database: string, command: string, signature: MessageType) {
        if (!signature)
            return app.schemaLess;
        const category: MessageCategory = signature.result ? "commands" : "messages";
        const param         = EntityEditor.getMessageArg("param", database, signature.param);
        const returnHtml    = EntityEditor.getReturnType(database, signature.result);
        const commandEl     = app.getSchemaCommand(database, category, command);        
        const el =
        `<span title="command parameter type">
            ${commandEl}
            (${param})
        </span>
        ${returnHtml}`;
        return el;
    }

    private static getReturnType(database: string, returnType: FieldType) : string {
        if (!returnType)
            return "&nbsp;: void";
        const resultType = app.getTypeLabel(database, returnType);
        return `<span style="opacity: 0.5;">&nbsp;:&nbsp;</span><span title="command result type">${resultType}</span>`;
    }

    private static getMessageArg(name: string, database: string, fieldType: FieldType) : string {
        if (!fieldType)
            return "";
        const type = app.getTypeLabel(database, fieldType);
        return `<span style="opacity: 0.5;">${name}: </span><span>${type}</span>`;
    }

    public tryFollowLink(value: string, column: number, line: number) : void {
        try {
            JSON.parse(value);  // early out invalid JSON
            const ast               = parseAst(value);
            const database          = this.entityIdentity.database;
            const containerSchema   = app.getContainerSchema(database, this.entityIdentity.container);

            let entity: Resource;
            EntityEditor.addRelationsFromAst(ast, containerSchema, (value, container) => {
                if (entity || value.type != "Literal")
                    return;
                const start = value.loc.start;
                const end   = value.loc.end;
                if (start.line <= line && start.column <= column && line <= end.line && column <= end.column) {
                    // console.log(`${resolvedDef.databaseName}/${resolvedDef.containerName}/${value.value}`);
                    const literalValue = value.value as string;
                    entity = { database: database, container: container, ids: [literalValue] };
                }
            });
            if (entity) {
                this.loadEntities(entity, false, null);
            }
        } catch (error) {
            writeResult.innerHTML = `<span style="color:#FF8C00">Follow link failed: ${error}</code>`;
        }
    }
}
