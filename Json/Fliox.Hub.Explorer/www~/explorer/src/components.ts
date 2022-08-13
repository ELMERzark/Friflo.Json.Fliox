import { DbContainers } from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";
import { createEl } from "./types.js";


export class ClusterTree {
    private selectedTreeEl: HTMLElement;

    onSelectDatabase  : (databaseName: string, classList: DOMTokenList) => void;
    onSelectContainer : (databaseName: string, containerName: string, classList: DOMTokenList) => void;

    private selectTreeElement(element: HTMLElement) {
        if (this.selectedTreeEl)
            this.selectedTreeEl.classList.remove("selected");
        this.selectedTreeEl =element;
        element.classList.add("selected");
    }

    public createClusterUl(dbContainers: DbContainers[]) : HTMLUListElement {
        const ulCluster = createEl('ul');
        ulCluster.onclick = (ev) => {
            const path          = ev.composedPath() as HTMLElement[];
            const databaseEl    = ClusterTree.findTreeEl (path, "clusterDatabase");
            const caretEl       = ClusterTree.findTreeEl (path, "caret");
            if (caretEl) {
                databaseEl.parentElement.classList.toggle("active");
                return;
            }
            const databaseName  = databaseEl.childNodes[1].textContent;
            const treeEl        = databaseEl.parentElement;
            if (this.selectedTreeEl == databaseEl) {
                if (treeEl.classList.contains("active"))
                    treeEl.classList.remove("active");
                else 
                    treeEl.classList.add("active");
                return;
            }
            treeEl.classList.add("active");
            this.selectTreeElement(databaseEl);
            this.onSelectDatabase(databaseName, path[0].classList);
        };
        let firstDatabase = true;
        for (const dbContainer of dbContainers) {
            const liDatabase = createEl('li');

            const divDatabase           = createEl('div');
            const dbCaret               = createEl('div');
            dbCaret.classList.value     = "caret";
            const dbLabel               = createEl('span');
            dbLabel.innerText           = dbContainer.id;
            divDatabase.title           = "database";
            divDatabase.className       = "clusterDatabase";

            const containerTag    = createEl('span');
            containerTag.innerHTML= "sub";
            containerTag.className = "sub";

            divDatabase.append(dbCaret);
            divDatabase.append(dbLabel);
            divDatabase.append(containerTag);
            liDatabase.appendChild(divDatabase);
            ulCluster.append(liDatabase);
            if (firstDatabase) {
                firstDatabase = false;
                liDatabase.classList.add("active");
                this.selectTreeElement(divDatabase);
            }
            const ulContainers = createEl('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                const path              = ev.composedPath() as HTMLElement[];
                const containerEl       = ClusterTree.findTreeEl (path, "clusterContainer");
                if (!containerEl)
                    return;
                const databaseEl        = containerEl.parentNode.parentNode;
                this.selectTreeElement(containerEl);
                const containerNameDiv  = containerEl.children[0] as HTMLDivElement;
                const containerName     = containerNameDiv.innerText.trim();
                const databaseName      = databaseEl.childNodes[0].childNodes[1].textContent;
                this.onSelectContainer(databaseName, containerName, path[0].classList);
            };
            liDatabase.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                const liContainer       = createEl('li');
                liContainer.title       = "container";
                liContainer.className   = "clusterContainer";
                const containerLabel    = createEl('div');
                containerLabel.innerHTML= "&nbsp;" + containerName;
                liContainer.append(containerLabel);

                const containerTag    = createEl('div');
                containerTag.innerHTML= "sub";
                containerTag.className = "sub";
                liContainer.append(containerTag);

                ulContainers.append(liContainer);
            }
        }
        return ulCluster;
    }

    private static findTreeEl(path: HTMLElement[], itemClass: string) {
        for (const el of path) {
            if (el.classList?.contains(itemClass))
                return el;
        }
        return null;
    }
}