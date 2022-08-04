/* Tables */
CREATE TABLE "Client" (
    "Id" bigint NOT NULL,
    "Secret" character varying(200) NOT NULL,
    "Name" character varying(50) NOT NULL,
    "CallbackUrl" character varying(500) NULL,
    "TokenExpireSeconds" integer NULL,
    "RefreshTokenExpireSeconds" integer NULL,
    "CodeExpireSeconds" integer NULL,
    "Disabled" boolean NOT NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Client" PRIMARY KEY ("Id")
);


CREATE TABLE "ClientExtendData" (
    "Id" bigint NOT NULL,
    "Key" character varying(50) NOT NULL,
    "Value" character varying(1000) NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_ClientExtendData" PRIMARY KEY ("Id", "Key"),
    CONSTRAINT "FK_ClientExtendData_Client_Id" FOREIGN KEY ("Id") REFERENCES "Client" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ClientRole" (
    "Id" bigint NOT NULL,
    "RoleId" bigint NOT NULL,
    "ExpireDate" timestamp with time zone NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_ClientRole" PRIMARY KEY ("Id", "RoleId"),
    CONSTRAINT "FK_ClientRole_Client_Id" FOREIGN KEY ("Id") REFERENCES "Client" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ClientRole_Role_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Role" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Permission" (
    "Id" bigint NOT NULL,
    "RoleId" bigint NOT NULL,
    "FunctionId" character varying(50) NOT NULL,
    "PermissionType" integer NOT NULL,
    "Allowed" boolean NOT NULL,
    "Priority" integer NOT NULL,
    "ReplaceExtendData" boolean NOT NULL,
    "ExpireDate" timestamp with time zone NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Permission" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Permission_Role_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Role" ("Id") ON DELETE CASCADE
);


CREATE TABLE "PermissionExtendData" (
    "Id" bigint NOT NULL,
    "Key" character varying(50) NOT NULL,
    "Value" character varying(200) NOT NULL,
    "PermissionType" integer NOT NULL,
    "Allowed" boolean NOT NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_PermissionExtendData" PRIMARY KEY ("Id", "Key", "Value"),
    CONSTRAINT "FK_PermissionExtendData_Permission_Id" FOREIGN KEY ("Id") REFERENCES "Permission" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Role" (
    "Id" bigint NOT NULL,
    "Name" character varying(50) NOT NULL,
    "Priority" integer NOT NULL,
    "Disabled" boolean NOT NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Role" PRIMARY KEY ("Id")
);


CREATE TABLE "RoleExtendData" (
    "Id" bigint NOT NULL,
    "Key" character varying(50) NOT NULL,
    "Value" character varying(1000) NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_RoleExtendData" PRIMARY KEY ("Id", "Key"),
    CONSTRAINT "FK_RoleExtendData_Role_Id" FOREIGN KEY ("Id") REFERENCES "Role" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Route" (
    "Id" bigint NOT NULL,
    "GroupId" bigint NOT NULL,
    "Protocol" character varying(10) NOT NULL,
    "HttpMethod" character varying(10) NOT NULL,
    "RelativePath" character varying(200) NOT NULL,
    "Template" character varying(200) NOT NULL,
    "FunctionId" character varying(50) NOT NULL,
    "NativePermission" integer NOT NULL,
    "AllowAnonymous" boolean NOT NULL,
    "Tag" character varying(200) NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_Route" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Route_RouteGroup_GroupId" FOREIGN KEY ("GroupId") REFERENCES "RouteGroup" ("Id") ON DELETE CASCADE
);


CREATE TABLE "RouteGroup" (
    "Id" bigint NOT NULL,
    "Name" character varying(100) NOT NULL,
    "BaseUrl" character varying(200) NOT NULL,
    "ForwarderRequestVersion" character varying(10) NULL,
    "ForwarderHttpVersionPolicy" integer NULL,
    "ForwarderActivityTimeout" interval NULL,
    "ForwarderAllowResponseBuffering" boolean NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_RouteGroup" PRIMARY KEY ("Id")
);


CREATE TABLE "RouteValue" (
    "Id" bigint NOT NULL,
    "Key" character varying(50) NOT NULL,
    "Value" character varying(1000) NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_RouteValue" PRIMARY KEY ("Id", "Key"),
    CONSTRAINT "FK_RouteValue_Route_Id" FOREIGN KEY ("Id") REFERENCES "Route" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Token" (
    "Id" bigint NOT NULL,
    "ResourceType" integer NOT NULL,
    "ResourceId" character varying(50) NOT NULL,
    "TokenType" character varying(50) NOT NULL,
    "AccessToken" character varying(2048) NOT NULL,
    "ExpiresIn" integer NULL,
    "Scope" character varying(2048) NULL,
    "RefreshToken" character varying(2048) NULL,
    "Disabled" boolean NOT NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL
) PARTITION BY RANGE ("CreationDate");


CREATE TABLE "User" (
    "Id" bigint NOT NULL,
    "Username" character varying(50) NOT NULL,
    "NormalizedUsername" character varying(50) NOT NULL,
    "Password" character varying(200) NOT NULL,
    "Email" character varying(100) NULL,
    "NormalizedEmail" character varying(100) NULL,
    "EmailConfirmed" boolean NOT NULL,
    "PhoneNumber" character varying(50) NULL,
    "PhoneNumberConfirmed" boolean NOT NULL,
    "Otp" character varying(50) NULL,
    "OtpBound" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "RequiredChangePassword" boolean NOT NULL,
    "TokenExpireSeconds" integer NULL,
    "RefreshTokenExpireSeconds" integer NULL,
    "CodeExpireSeconds" integer NULL,
    "AccessFailedCount" integer NOT NULL,
    "LastSignInDate" timestamp with time zone NULL,
    "LastSignInIp" character varying(50) NULL,
    "Disabled" boolean NOT NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_User" PRIMARY KEY ("Id")
);


CREATE TABLE "UserExtendData" (
    "Id" bigint NOT NULL,
    "Key" character varying(50) NOT NULL,
    "Value" character varying(1000) NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_UserExtendData" PRIMARY KEY ("Id", "Key"),
    CONSTRAINT "FK_UserExtendData_User_Id" FOREIGN KEY ("Id") REFERENCES "User" ("Id") ON DELETE CASCADE
);


CREATE TABLE "UserExternalLogin" (
    "Id" bigint NOT NULL,
    "Provider" character varying(50) NOT NULL,
    "UniqueId" character varying(100) NOT NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_UserExternalLogin" PRIMARY KEY ("Id", "Provider"),
    CONSTRAINT "FK_UserExternalLogin_User_Id" FOREIGN KEY ("Id") REFERENCES "User" ("Id") ON DELETE CASCADE
);


CREATE TABLE "UserRole" (
    "Id" bigint NOT NULL,
    "RoleId" bigint NOT NULL,
    "ExpireDate" timestamp with time zone NULL,
    "CreationDate" timestamp with time zone NOT NULL,
    "CreatorId" bigint NOT NULL,
    "ModificationDate" timestamp with time zone NOT NULL,
    "ModifierId" bigint NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_UserRole" PRIMARY KEY ("Id", "RoleId"),
    CONSTRAINT "FK_UserRole_Role_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Role" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRole_User_Id" FOREIGN KEY ("Id") REFERENCES "User" ("Id") ON DELETE CASCADE
);


/* Indexes */
CREATE INDEX "IX_Client_Disabled" ON "Client" ("Disabled");


CREATE UNIQUE INDEX "IX_Client_Name" ON "Client" ("Name");


CREATE INDEX "IX_ClientExtendData_Key" ON "ClientExtendData" ("Key");


CREATE INDEX "IX_ClientExtendData_Value" ON "ClientExtendData" ("Value");


CREATE INDEX "IX_ClientRole_RoleId" ON "ClientRole" ("RoleId");


CREATE INDEX "IX_Permission_Allowed" ON "Permission" ("Allowed");


CREATE INDEX "IX_Permission_ExpireDate" ON "Permission" ("ExpireDate");


CREATE INDEX "IX_Permission_FunctionId" ON "Permission" ("FunctionId");


CREATE UNIQUE INDEX "IX_Permission_Id_FunctionId_PermissionType_Allowed" ON "Permission" ("Id", "FunctionId", "PermissionType", "Allowed");


CREATE INDEX "IX_Permission_PermissionType" ON "Permission" ("PermissionType");


CREATE INDEX "IX_Permission_RoleId" ON "Permission" ("RoleId");


CREATE INDEX "IX_Role_Name" ON "Role" ("Name");


CREATE INDEX "IX_RoleExtendData_Key" ON "RoleExtendData" ("Key");


CREATE INDEX "IX_RoleExtendData_Value" ON "RoleExtendData" ("Value");


CREATE INDEX "IX_Route_AllowAnonymous" ON "Route" ("AllowAnonymous");


CREATE INDEX "IX_Route_FunctionId" ON "Route" ("FunctionId");


CREATE INDEX "IX_Route_GroupId" ON "Route" ("GroupId");


CREATE INDEX "IX_Route_HttpMethod" ON "Route" ("HttpMethod");


CREATE UNIQUE INDEX "IX_Route_HttpMethod_RelativePath" ON "Route" ("HttpMethod", "RelativePath");


CREATE INDEX "IX_Route_NativePermission" ON "Route" ("NativePermission");


CREATE INDEX "IX_Route_Protocol" ON "Route" ("Protocol");


CREATE INDEX "IX_Route_RelativePath" ON "Route" ("RelativePath");


CREATE INDEX "IX_Route_Tag" ON "Route" ("Tag");


CREATE INDEX "IX_Route_Template" ON "Route" ("Template");


CREATE INDEX "IX_RouteGroup_Name" ON "RouteGroup" ("Name");


CREATE INDEX "IX_RouteValue_Key" ON "RouteValue" ("Key");


CREATE INDEX "IX_RouteValue_Value" ON "RouteValue" ("Value");


CREATE INDEX "IX_Token_AccessToken" ON "Token" ("AccessToken");


CREATE INDEX "IX_Token_Disabled" ON "Token" ("Disabled");


CREATE INDEX "IX_Token_ExpiresIn" ON "Token" ("ExpiresIn");


CREATE INDEX "IX_Token_RefreshToken" ON "Token" ("RefreshToken");


CREATE INDEX "IX_Token_ResourceId" ON "Token" ("ResourceId");


CREATE INDEX "IX_Token_ResourceType" ON "Token" ("ResourceType");


CREATE INDEX "IX_Token_TokenType" ON "Token" ("TokenType");


CREATE INDEX "IX_User_Disabled" ON "User" ("Disabled");


CREATE INDEX "IX_User_Email" ON "User" ("Email");


CREATE INDEX "IX_User_NormalizedEmail" ON "User" ("NormalizedEmail");


CREATE UNIQUE INDEX "IX_User_NormalizedUsername" ON "User" ("NormalizedUsername");


CREATE INDEX "IX_User_PhoneNumber" ON "User" ("PhoneNumber");


CREATE UNIQUE INDEX "IX_User_Username" ON "User" ("Username");


CREATE INDEX "IX_UserExtendData_Key" ON "UserExtendData" ("Key");


CREATE INDEX "IX_UserExtendData_Value" ON "UserExtendData" ("Value");


CREATE INDEX "IX_UserExternalLogin_Provider" ON "UserExternalLogin" ("Provider");


CREATE INDEX "IX_UserExternalLogin_UniqueId" ON "UserExternalLogin" ("UniqueId");


CREATE INDEX "IX_UserRole_RoleId" ON "UserRole" ("RoleId");


/* Functions */
CREATE OR REPLACE FUNCTION fn_token_partition_table(_customDate timestamptz default null)
  RETURNS INT8 
  LANGUAGE PLPGSQL
AS $$
DECLARE tableName text; currentDate timestamptz; beginDate timestamptz; endDate timestamptz;
BEGIN
  currentDate = COALESCE(_customDate, now());
  beginDate = to_char(currentDate, 'YYYY-MM-01"T"00:00:00"Z"')::timestamptz + (0     || ' month')::interval;
  endDate =   to_char(currentDate, 'YYYY-MM-01"T"00:00:00"Z"')::timestamptz + (0 + 1 || ' month')::interval;
  tableName = 'Token_'||to_char(currentDate, 'YYYYMM');

  EXECUTE 'CREATE TABLE IF NOT EXISTS "'||tableName||'" PARTITION OF "Token" FOR VALUES FROM ('''||to_char(beginDate, 'YYYY-MM-DD"T"HH24:MI:SS"Z"')||''') TO ('''||to_char(endDate, 'YYYY-MM-DD"T"HH24:MI:SS"Z"')||''')';
  EXECUTE 'ALTER TABLE "'||tableName||'" ADD CONSTRAINT "PK_'||tableName||'" PRIMARY KEY ("Id")';

  RETURN 1;
END;
$$