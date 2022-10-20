DROP TABLE IF EXISTS `weather`.`Stations`;
DROP TABLE IF EXISTS `weather`.`Air_Humidity`;
DROP TABLE IF EXISTS `weather`.`Atmos_Pressure`;
DROP TABLE IF EXISTS `weather`.`Pavement`;
DROP TABLE IF EXISTS `weather`.`Precipitation`;
DROP TABLE IF EXISTS `weather`.`Snow`;
DROP TABLE IF EXISTS `weather`.`Wind`;

CREATE TABLE `weather`.`Stations` (
  `StationID` VARCHAR(80) NOT NULL,
  `StationName` VARCHAR(80) NULL,
  `LastUploadTime` DATETIME,
  `ConnectionString` VARCHAR(256) NULL,
  PRIMARY KEY (`StationID`));
CREATE INDEX st_stationID ON `weather`.`Stations`(`StationID`, `LastUploadTime`);

CREATE TABLE `weather`.`Air_Humidity` (
  `TmStamp` DATETIME,
  `RecNum` BIGINT NOT NULL AUTO_INCREMENT,
  `StationID` VARCHAR(80) NOT NULL,
  `Identifier` INT,
  `MaxAirTemp1` FLOAT(10,2),
  `CurAirTemp1` FLOAT(10,2),
  `MinAirTemp1` FLOAT(10,2),
  `AirTempQ`  FLOAT(10,2),
  `AirTemp2` FLOAT(10,2),
  `AirTemp2Q` FLOAT(10,2),
  `RH` FLOAT(10,2),
  `Dew_Point` FLOAT(10,2),
  PRIMARY KEY (`RecNum`)); 
CREATE INDEX ah_stationID ON `weather`.`Air_Humidity`(`StationID`, `TmStamp`);
    
CREATE TABLE `weather`.`Atmos_Pressure` (
  `TmStamp` DATETIME,
  `RecNum` BIGINT NOT NULL AUTO_INCREMENT,
  `StationID` VARCHAR(80) NOT NULL,
  `Identifier` INT,
  `AtmPressure` FLOAT(10,2),
  PRIMARY KEY (`RecNum`));
CREATE INDEX ap_stationID ON `weather`.`Atmos_Pressure`(`StationID`, `TmStamp`);

CREATE TABLE `weather`.`Pavement` (
  `TmStamp` DATETIME,
  `RecNum` BIGINT NOT NULL AUTO_INCREMENT,
  `StationID` VARCHAR(80) NOT NULL,
  `Identifier` INT,
  `PvmntTemp1` FLOAT(10,2),
  `PavementQ1` FLOAT(10,2),
  `AltPaveTemp1` FLOAT(10,2),
  `FrzPntTemp1` FLOAT(10,2),
  `FrzPntTemp1Q` FLOAT(10,2),
  `PvmntCond1Q` FLOAT(10,2),
  `SbAsphltTemp` FLOAT(10,2),
  `PvBaseTemp1` FLOAT(10,2),
  `PvBaseTemp1Q` FLOAT(10,2),
  `PvmntSrfCvTh` FLOAT(10,2),
  `PvmntSrfCvThQ` FLOAT(10,2),
  PRIMARY KEY (`RecNum`));
CREATE INDEX p_stationID ON `weather`.`Pavement`(`StationID`, `TmStamp`);

CREATE TABLE `weather`.`Precipitation` (
  `TmStamp` DATETIME,
  `RecNum` BIGINT NOT NULL AUTO_INCREMENT,
  `StationID` VARCHAR(80) NOT NULL,
  `Identifier` INT,
  `GaugeTot` FLOAT(10,2),
  `NewPrecip` FLOAT(10,2),
  `HrlyPrecip` FLOAT(10,2),
  `PrecipGaugeQ` FLOAT(10,2),
  `PrecipDetRatio` FLOAT(10,2),
  `PrecipDetQ` FLOAT(10,2),
  PRIMARY KEY (`RecNum`));
CREATE INDEX p2_stationID ON `weather`.`Precipitation`(`StationID`, `TmStamp`);

CREATE TABLE `weather`.`Snow` (
  `TmStamp` DATETIME,
  `RecNum` BIGINT NOT NULL AUTO_INCREMENT,
  `StationID` VARCHAR(80) NOT NULL,
  `Identifier` INT,
  `HS` FLOAT(10,2),
  `HStd` FLOAT(10,2),
  `HrlySnow` FLOAT(10,2),
  `SnowQ` FLOAT(10,2),
  PRIMARY KEY (`RecNum`));
CREATE INDEX s_stationID ON `weather`.`Snow`(`StationID`, `TmStamp`);

CREATE TABLE `weather`.`Wind` (
  `TmStamp` DATETIME,
  `RecNum` BIGINT NOT NULL AUTO_INCREMENT,
  `StationID` VARCHAR(80) NOT NULL,
  `Identifier` INT,
  `MaxWindSpd` FLOAT(10,2),
  `MeanWindSpd` FLOAT(10,2),
  `WindSpd` FLOAT(10,2),
  `WindSpdQ` FLOAT(10,2),
  `MeanWindDir` FLOAT(10,2),
  `StDevWind` FLOAT(10,2),
  `WindDir` FLOAT(10,2),
  `DerimeStat` FLOAT(10,2),
  PRIMARY KEY (`RecNum`));
CREATE INDEX w_stationID ON `weather`.`Wind`(`StationID`, `TmStamp`);
