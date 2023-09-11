--CREATE EXTENSION IF NOT EXISTS pg_trgm;


CREATE TABLE "Client" (
  "Id" bigint NOT NULL,
  "Secret" character varying(200) NOT NULL,
  "Name" character varying(50) NOT NULL,
  "CallbackUrl" character varying(500) NULL,
  "AllowedRefreshToken" boolean NOT NULL,
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


CREATE TABLE "Permission" (
  "Id" bigint NOT NULL,
  "Name" character varying(50) NOT NULL,
  "Priority" integer NOT NULL,
  "Disabled" boolean NOT NULL,
  "State" character varying(50) NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_Permission" PRIMARY KEY ("Id")
);


CREATE TABLE "Role" (
  "Id" bigint NOT NULL,
  "Name" character varying(50) NOT NULL,
  "Disabled" boolean NOT NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_Role" PRIMARY KEY ("Id")
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


CREATE TABLE "Token" (
  "Id" bigint NOT NULL,
  "ResourceType" integer NOT NULL,
  "ResourceId" character varying(50) NOT NULL,
  "TokenType" character varying(50) NOT NULL,
  "AccessToken" character varying(2048) NOT NULL,
  "ExpiresIn" integer NOT NULL,
  "ExpiresAt" bigint NOT NULL,
  "Scope" character varying(2048) NULL,
  "RefreshToken" character varying(2048) NULL,
  "RefreshExpiresIn" integer NULL,
  "RefreshExpiresAt" bigint NULL,
  "Revoked" integer NOT NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_Token" PRIMARY KEY ("Id")
);


CREATE TABLE "User" (
  "Id" bigint NOT NULL,
  "Username" character varying(50) NOT NULL,
  "NormalizedUsername" character varying(50) NOT NULL,
  "DisplayName" character varying(50) NOT NULL,
  "NormalizedDisplayName" character varying(50) NOT NULL,
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
  "AllowedRefreshToken" boolean NOT NULL,
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


CREATE TABLE "ClientExtendData" (
  "Id" bigint NOT NULL,
  "Key" character varying(50) NOT NULL,
  "Value" character varying(200) NOT NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_ClientExtendData" PRIMARY KEY ("Id", "Key"),
  CONSTRAINT "FK_ClientExtendData_Client_Id" FOREIGN KEY ("Id") REFERENCES "Client" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Rule" (
  "Id" bigint NOT NULL,
  "PermissionId" bigint NOT NULL,
  "FunctionId" character varying(50) NOT NULL,
  "PermissionType" integer NOT NULL,
  "Allowed" boolean NOT NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_Rule" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_Rule_Permission_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permission" ("Id") ON DELETE CASCADE
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


CREATE TABLE "RoleExtendData" (
  "Id" bigint NOT NULL,
  "Key" character varying(50) NOT NULL,
  "Value" character varying(200) NOT NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_RoleExtendData" PRIMARY KEY ("Id", "Key"),
  CONSTRAINT "FK_RoleExtendData_Role_Id" FOREIGN KEY ("Id") REFERENCES "Role" ("Id") ON DELETE CASCADE
);


CREATE TABLE "RolePermission" (
  "Id" bigint NOT NULL,
  "PermissionId" bigint NOT NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_RolePermission" PRIMARY KEY ("Id", "PermissionId"),
  CONSTRAINT "FK_RolePermission_Permission_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permission" ("Id") ON DELETE CASCADE,
  CONSTRAINT "FK_RolePermission_Role_Id" FOREIGN KEY ("Id") REFERENCES "Role" ("Id") ON DELETE CASCADE
);


CREATE TABLE "RolePermissionCondition" (
  "Id" bigint NOT NULL,
  "RoleId" bigint NOT NULL,
  "PermissionId" bigint NOT NULL,
  "Group" character varying(50) NULL,
  "Key" character varying(50) NOT NULL,
  "Value" character varying(200) NOT NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_RolePermissionCondition" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_RolePermissionCondition_Permission_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permission" ("Id") ON DELETE CASCADE,
  CONSTRAINT "FK_RolePermissionCondition_Role_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Role" ("Id") ON DELETE CASCADE
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


CREATE TABLE "UserExtendData" (
  "Id" bigint NOT NULL,
  "Key" character varying(50) NOT NULL,
  "Value" character varying(200) NOT NULL,
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


CREATE TABLE "UserPermissionCondition" (
  "Id" bigint NOT NULL,
  "UserId" bigint NOT NULL,
  "PermissionId" bigint NOT NULL,
  "Group" character varying(50) NULL,
  "Key" character varying(50) NOT NULL,
  "Value" character varying(200) NOT NULL,
  "ExpireDate" timestamp with time zone NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_UserPermissionCondition" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_UserPermissionCondition_Permission_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permission" ("Id") ON DELETE CASCADE,
  CONSTRAINT "FK_UserPermissionCondition_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE CASCADE
);


CREATE TABLE "RouteValue" (
  "Id" bigint NOT NULL,
  "Key" character varying(50) NOT NULL,
  "Value" character varying(200) NOT NULL,
  "CreationDate" timestamp with time zone NOT NULL,
  "CreatorId" bigint NOT NULL,
  "ModificationDate" timestamp with time zone NOT NULL,
  "ModifierId" bigint NOT NULL,
  "Version" bigint NOT NULL,
  CONSTRAINT "PK_RouteValue" PRIMARY KEY ("Id", "Key"),
  CONSTRAINT "FK_RouteValue_Route_Id" FOREIGN KEY ("Id") REFERENCES "Route" ("Id") ON DELETE CASCADE
);


CREATE INDEX "IX_ClientExtendData_Key" ON "ClientExtendData" ("Key");
CREATE INDEX "IX_ClientExtendData_Value" ON "ClientExtendData" ("Value");
CREATE INDEX "IX_ClientRole_RoleId" ON "ClientRole" ("RoleId");
CREATE INDEX "IX_Client_Disabled" ON "Client" ("Disabled");
CREATE INDEX "IX_Permission_Disabled" ON "Permission" ("Disabled");
CREATE INDEX "IX_Permission_State" ON "Permission" ("State");
CREATE INDEX "IX_RoleExtendData_Key" ON "RoleExtendData" ("Key");
CREATE INDEX "IX_RoleExtendData_Value" ON "RoleExtendData" ("Value");
CREATE INDEX "IX_RolePermissionCondition_Group" ON "RolePermissionCondition" ("Group");
CREATE INDEX "IX_RolePermissionCondition_PermissionId" ON "RolePermissionCondition" ("PermissionId");
CREATE INDEX "IX_RolePermissionCondition_RoleId" ON "RolePermissionCondition" ("RoleId");
CREATE INDEX "IX_RolePermission_PermissionId" ON "RolePermission" ("PermissionId");
CREATE INDEX "IX_Role_Disabled" ON "Role" ("Disabled");
CREATE INDEX "IX_RouteGroup_Name" ON "RouteGroup" ("Name");
CREATE INDEX "IX_RouteValue_Key" ON "RouteValue" ("Key");
CREATE INDEX "IX_RouteValue_Value" ON "RouteValue" ("Value");
CREATE INDEX "IX_Route_AllowAnonymous" ON "Route" ("AllowAnonymous");
CREATE INDEX "IX_Route_FunctionId" ON "Route" ("FunctionId");
CREATE INDEX "IX_Route_GroupId" ON "Route" ("GroupId");
CREATE INDEX "IX_Route_HttpMethod" ON "Route" ("HttpMethod");
CREATE INDEX "IX_Route_NativePermission" ON "Route" ("NativePermission");
CREATE INDEX "IX_Route_Protocol" ON "Route" ("Protocol");
CREATE INDEX "IX_Route_RelativePath" ON "Route" ("RelativePath");
CREATE INDEX "IX_Route_Tag" ON "Route" ("Tag");
CREATE INDEX "IX_Route_Template" ON "Route" ("Template");
CREATE INDEX "IX_Rule_Allowed" ON "Rule" ("Allowed");
CREATE INDEX "IX_Rule_FunctionId" ON "Rule" ("FunctionId");
CREATE INDEX "IX_Rule_PermissionId" ON "Rule" ("PermissionId");
CREATE INDEX "IX_Rule_PermissionType" ON "Rule" ("PermissionType");
CREATE INDEX "IX_Token_AccessToken" ON "Token" ("AccessToken");
CREATE INDEX "IX_Token_Revoked" ON "Token" ("Revoked");
CREATE INDEX "IX_Token_ExpiresIn" ON "Token" ("ExpiresIn");
CREATE INDEX "IX_Token_ExpiresAt" ON "Token" ("ExpiresAt");
CREATE INDEX "IX_Token_RefreshToken" ON "Token" ("RefreshToken");
CREATE INDEX "IX_Token_RefreshExpiresIn" ON "Token" ("RefreshExpiresIn");
CREATE INDEX "IX_Token_RefreshExpiresAt" ON "Token" ("RefreshExpiresAt");
CREATE INDEX "IX_Token_ResourceId" ON "Token" ("ResourceId");
CREATE INDEX "IX_Token_ResourceType" ON "Token" ("ResourceType");
CREATE INDEX "IX_Token_TokenType" ON "Token" ("TokenType");
CREATE INDEX "IX_UserExtendData_Key" ON "UserExtendData" ("Key");
CREATE INDEX "IX_UserExtendData_Value" ON "UserExtendData" ("Value");
CREATE INDEX "IX_UserExternalLogin_Provider" ON "UserExternalLogin" ("Provider");
CREATE INDEX "IX_UserExternalLogin_UniqueId" ON "UserExternalLogin" ("UniqueId");
CREATE INDEX "IX_UserPermissionCondition_ExpireDate" ON "UserPermissionCondition" ("ExpireDate");
CREATE INDEX "IX_UserPermissionCondition_Group" ON "UserPermissionCondition" ("Group");
CREATE INDEX "IX_UserPermissionCondition_PermissionId" ON "UserPermissionCondition" ("PermissionId");
CREATE INDEX "IX_UserPermissionCondition_UserId" ON "UserPermissionCondition" ("UserId");
CREATE INDEX "IX_UserRole_RoleId" ON "UserRole" ("RoleId");
CREATE INDEX "IX_UserRole_Id_ExpireDate" ON "UserRole" ("Id", "ExpireDate");
CREATE INDEX "IX_User_Disabled" ON "User" ("Disabled");
CREATE INDEX "IX_User_DisplayName" ON "User" ("DisplayName");
CREATE INDEX "IX_User_Email" ON "User" ("Email");
CREATE INDEX "IX_User_NormalizedEmail" ON "User" ("NormalizedEmail");
CREATE INDEX "IX_User_PhoneNumber" ON "User" ("PhoneNumber");
CREATE UNIQUE INDEX "IX_User_Username" ON "User" ("Username");
CREATE UNIQUE INDEX "IX_User_NormalizedUsername" ON "User" ("NormalizedUsername");
CREATE UNIQUE INDEX "IX_Client_Name" ON "Client" ("Name");
CREATE UNIQUE INDEX "IX_Permission_Name" ON "Permission" ("Name");
CREATE UNIQUE INDEX "IX_RolePermissionCondition_RoleId_PermissionId_Group_Key_Value" ON "RolePermissionCondition" ("RoleId", "PermissionId", "Group", "Key", "Value");
CREATE UNIQUE INDEX "IX_Role_Name" ON "Role" ("Name");
CREATE UNIQUE INDEX "IX_Route_HttpMethod_RelativePath" ON "Route" ("HttpMethod", "RelativePath");
CREATE UNIQUE INDEX "IX_Rule_PermissionId_FunctionId_PermissionType_Allowed" ON "Rule" ("PermissionId", "FunctionId", "PermissionType", "Allowed");
CREATE UNIQUE INDEX "IX_UserPermissionCondition_UserId_PermissionId_Group_Key_Value" ON "UserPermissionCondition" ("UserId", "PermissionId", "Group", "Key", "Value");


/* Functions */
CREATE OR REPLACE FUNCTION public.fn_prune_token()
 RETURNS int
 LANGUAGE plpgsql
AS $function$
DECLARE currentDate DECIMAL; result int;
BEGIN
  currentDate = EXTRACT (EPOCH FROM CURRENT_TIMESTAMP);

  WITH deleted AS (
    DELETE FROM "Token" WHERE ("ExpiresAt" < currentDate AND "RefreshExpiresAt" IS NULL) OR ("ExpiresAt" < currentDate AND "RefreshExpiresAt" < currentDate) RETURNING *
  ) SELECT count(*) into result FROM deleted;

  return result;
END;
$function$;

CREATE OR REPLACE FUNCTION public.fn_token_partition_table(_customdate timestamp with time zone DEFAULT NULL::timestamp with time zone, _months bigint DEFAULT NULL::bigint)
  RETURNS bigint
  LANGUAGE plpgsql
AS $function$
DECLARE tableName text; cmd text; currentDate timestamptz; beginDate timestamptz; endDate timestamptz; months int8;
BEGIN
  currentDate = COALESCE(_customDate, now());
  beginDate   = to_char(currentDate, 'YYYY-MM-01')::timestamptz;
  months      = COALESCE(_months, 1);

  FOR i IN 0..months LOOP
    endDate   = to_char(beginDate, 'YYYY-MM-01')::timestamptz + (1 || ' month')::interval;
    tableName = 'Token_'||to_char(beginDate, 'YYYYMM');

    IF EXISTS (SELECT 1 FROM information_schema."tables" WHERE "table_name" = tableName) THEN
      beginDate = endDate;
      CONTINUE;
    END IF;

    cmd = 'CREATE TABLE IF NOT EXISTS "'||tableName||'" PARTITION OF "Token" FOR VALUES FROM ('''||to_char(beginDate, 'YYYY-MM-DD')||''') TO ('''||to_char(endDate, 'YYYY-MM-DD')||''')';
    EXECUTE cmd;
    --RAISE NOTICE '%', cmd;

    cmd = 'ALTER TABLE "'||tableName||'" ADD CONSTRAINT "PK_'||tableName||'" PRIMARY KEY ("Id")';
    EXECUTE cmd;
    --RAISE NOTICE '%', cmd;

    beginDate = endDate;
  END LOOP;

  RETURN 1;
END;
$function$


-- select public.fn_token_partition_table();


-- Please switch to Db: postgres
-- CREATE EXTENSION pg_cron;
-- SELECT cron.schedule('token_partition_table', '0 0 */1 * *', 'SELECT fn_token_partition_table(null, 1)');
-- UPDATE cron.job SET
-- database = 'lctech_auth'
-- -- schedule = '0 0 */1 * *',
-- -- command = 'SELECT fn_token_partition_table(null, 1)',
-- -- jobname = '建立分表'
-- WHERE jobname = 'token_partition_table';

-- SELECT cron.schedule('prune_token', '0 19 * * *', 'SELECT fn_prune_token()');
-- UPDATE cron.job SET
-- database = 'lctech_auth'
-- WHERE jobname = 'prune_token';
