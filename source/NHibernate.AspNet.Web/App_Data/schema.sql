
    PRAGMA foreign_keys = OFF

    drop table if exists AspNetUsers

    drop table if exists AspNetUserRoles

    drop table if exists AspNetUserLogins

    drop table if exists AspNetRoles

    drop table if exists AspNetUserClaims

    PRAGMA foreign_keys = ON

    create table AspNetUsers (
        Id TEXT not null,
       UserName TEXT,
       PasswordHash TEXT,
       SecurityStamp TEXT,
       primary key (Id)
    )

    create table AspNetUserRoles (
        UserId TEXT not null,
       RoleId TEXT not null,
       constraint FK86803B282B87AB2A foreign key (RoleId) references AspNetRoles,
       constraint FK86803B28EA778823 foreign key (UserId) references AspNetUsers
    )

    create table AspNetUserLogins (
        UserId TEXT not null,
       LoginProvider TEXT,
       ProviderKey TEXT,
       constraint FKEF896DAEEA778823 foreign key (UserId) references AspNetUsers
    )

    create table AspNetRoles (
        Id TEXT not null,
       Name TEXT,
       primary key (Id)
    )

    create table AspNetUserClaims (
        Id  integer primary key autoincrement,
       ClaimType TEXT,
       ClaimValue TEXT,
       User_Id TEXT,
       constraint FKF4F7D992B13A912B foreign key (User_Id) references AspNetUsers
    )