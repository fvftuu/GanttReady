CREATE TABLE "Projects" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Projects" PRIMARY KEY AUTOINCREMENT,
    "Code" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "PlanStartDate" TEXT NOT NULL,
    "PlanEndDate" TEXT NOT NULL,
    "ActualStartDate" TEXT NULL,
    "ActualEndDate" TEXT NULL,
    "WorkingHoursPerDay" INTEGER NOT NULL,
    "WorkdaysPerWeek" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);


CREATE TABLE "ColumnDefinitions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ColumnDefinitions" PRIMARY KEY AUTOINCREMENT,
    "ProjectId" INTEGER NOT NULL,
    "ViewName" TEXT NOT NULL,
    "FieldName" TEXT NOT NULL,
    "DisplayName" TEXT NOT NULL,
    "Width" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "IsVisible" INTEGER NOT NULL,
    "IsEditable" INTEGER NOT NULL,
    CONSTRAINT "FK_ColumnDefinitions_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Resources" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Resources" PRIMARY KEY AUTOINCREMENT,
    "ProjectId" INTEGER NULL,
    "Code" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Type" INTEGER NOT NULL,
    "Unit" TEXT NOT NULL,
    "Quantity" TEXT NOT NULL,
    "UnitPrice" TEXT NOT NULL,
    "HourlyCost" TEXT NULL,
    "Notes" TEXT NULL,
    "ExtraData" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Resources_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE SET NULL
);


CREATE TABLE "Tasks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Tasks" PRIMARY KEY AUTOINCREMENT,
    "ProjectId" INTEGER NOT NULL,
    "Code" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "ParentTaskId" INTEGER NULL,
    "OutlineLevel" INTEGER NOT NULL,
    "PlanStartDate" TEXT NOT NULL,
    "PlanEndDate" TEXT NOT NULL,
    "PlanDuration" INTEGER NOT NULL,
    "ActualStartDate" TEXT NULL,
    "ActualEndDate" TEXT NULL,
    "ActualDuration" INTEGER NULL,
    "ResponsiblePerson" TEXT NULL,
    "CompletionPercentage" INTEGER NOT NULL,
    "IsMilestone" INTEGER NOT NULL,
    "IsManualSchedule" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "EarlyStart" INTEGER NULL,
    "EarlyFinish" INTEGER NULL,
    "LateStart" INTEGER NULL,
    "LateFinish" INTEGER NULL,
    "TotalFloat" INTEGER NULL,
    "FreeFloat" INTEGER NULL,
    "IsCritical" INTEGER NOT NULL,
    "ExtraData" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Tasks_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Tasks_Tasks_ParentTaskId" FOREIGN KEY ("ParentTaskId") REFERENCES "Tasks" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ResourceAssignments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ResourceAssignments" PRIMARY KEY AUTOINCREMENT,
    "TaskId" INTEGER NOT NULL,
    "ResourceId" INTEGER NOT NULL,
    "Quantity" TEXT NOT NULL,
    "Notes" TEXT NULL,
    CONSTRAINT "FK_ResourceAssignments_Resources_ResourceId" FOREIGN KEY ("ResourceId") REFERENCES "Resources" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ResourceAssignments_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("Id") ON DELETE CASCADE
);


CREATE TABLE "TaskRelations" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_TaskRelations" PRIMARY KEY AUTOINCREMENT,
    "ProjectId" INTEGER NOT NULL,
    "PredecessorTaskId" INTEGER NOT NULL,
    "SuccessorTaskId" INTEGER NOT NULL,
    "Type" INTEGER NOT NULL,
    "Lag" INTEGER NOT NULL,
    CONSTRAINT "FK_TaskRelations_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TaskRelations_Tasks_PredecessorTaskId" FOREIGN KEY ("PredecessorTaskId") REFERENCES "Tasks" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_TaskRelations_Tasks_SuccessorTaskId" FOREIGN KEY ("SuccessorTaskId") REFERENCES "Tasks" ("Id") ON DELETE RESTRICT
);


CREATE INDEX "IX_ColumnDefinitions_ProjectId_ViewName_SortOrder" ON "ColumnDefinitions" ("ProjectId", "ViewName", "SortOrder");


CREATE UNIQUE INDEX "IX_Projects_Code" ON "Projects" ("Code");


CREATE INDEX "IX_ResourceAssignments_ResourceId" ON "ResourceAssignments" ("ResourceId");


CREATE INDEX "IX_ResourceAssignments_TaskId" ON "ResourceAssignments" ("TaskId");


CREATE INDEX "IX_Resources_ProjectId" ON "Resources" ("ProjectId");


CREATE INDEX "IX_TaskRelations_PredecessorTaskId" ON "TaskRelations" ("PredecessorTaskId");


CREATE INDEX "IX_TaskRelations_ProjectId" ON "TaskRelations" ("ProjectId");


CREATE INDEX "IX_TaskRelations_SuccessorTaskId" ON "TaskRelations" ("SuccessorTaskId");


CREATE INDEX "IX_Tasks_ParentTaskId" ON "Tasks" ("ParentTaskId");


CREATE INDEX "IX_Tasks_ProjectId_SortOrder" ON "Tasks" ("ProjectId", "SortOrder");


