CREATE TABLE app_meta(key TEXT PRIMARY KEY, value TEXT NOT NULL);
INSERT INTO "app_meta" VALUES('schema_version','1.0');
INSERT INTO "app_meta" VALUES('seed_workbook','Director_Family_Travel_Smart_UI_v8_3_City_Flight_Coverage.xlsx');
INSERT INTO "app_meta" VALUES('seeded_at','2026-09-23T02:55:43');
CREATE TABLE cities(
 id INTEGER PRIMARY KEY AUTOINCREMENT,
 name TEXT NOT NULL,
 iata TEXT NOT NULL COLLATE NOCASE UNIQUE,
 is_active INTEGER NOT NULL DEFAULT 1
);
INSERT INTO "cities" VALUES(1,'Hyderabad','HYD',1);
INSERT INTO "cities" VALUES(2,'Chennai','MAA',1);
INSERT INTO "cities" VALUES(3,'Coimbatore','CJB',1);
INSERT INTO "cities" VALUES(4,'Bengaluru','BLR',1);
INSERT INTO "cities" VALUES(5,'Delhi','DEL',1);
INSERT INTO "cities" VALUES(6,'Mumbai','BOM',1);
INSERT INTO "cities" VALUES(7,'Pune','PNQ',1);
INSERT INTO "cities" VALUES(8,'Jodhpur','JDH',1);
CREATE TABLE flights(
 id INTEGER PRIMARY KEY AUTOINCREMENT,
 airline TEXT NOT NULL,
 flight_no TEXT NOT NULL,
 route_id INTEGER NOT NULL REFERENCES routes(id),
 departure_time TEXT NOT NULL,
 arrival_time TEXT NOT NULL,
 stops INTEGER NOT NULL DEFAULT 0,
 via_iata TEXT,
 display_value TEXT NOT NULL,
 source_note TEXT,
 is_active INTEGER NOT NULL DEFAULT 1,
 UNIQUE(flight_no,route_id,departure_time,arrival_time)
);
INSERT INTO "flights" VALUES(1,'IndiGo','6E-495',1,'07:10','08:25',0,NULL,'6E-495 | MAA-HYD | 07:10-08:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(2,'IndiGo','6E-834',1,'09:05','10:20',0,NULL,'6E-834 | MAA-HYD | 09:05-10:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(3,'IndiGo','6E-372',1,'09:55','11:10',0,NULL,'6E-372 | MAA-HYD | 09:55-11:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(4,'IndiGo','6E-6925',1,'10:40','11:55',0,NULL,'6E-6925 | MAA-HYD | 10:40-11:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(5,'Alliance Air','9I-894',1,'13:20','15:05',0,NULL,'9I-894 | MAA-HYD | 13:20-15:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(6,'IndiGo','6E-562',1,'18:20','19:35',0,NULL,'6E-562 | MAA-HYD | 18:20-19:35','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(7,'IndiGo','6E-193',1,'20:00','21:25',0,NULL,'6E-193 | MAA-HYD | 20:00-21:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(8,'IndiGo','6E-6487',1,'21:25','22:40',0,NULL,'6E-6487 | MAA-HYD | 21:25-22:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(9,'IndiGo','6E-243',2,'07:05','08:25',0,NULL,'6E-243 | HYD-MAA | 07:05-08:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(10,'IndiGo','6E-371',2,'07:55','09:20',0,NULL,'6E-371 | HYD-MAA | 07:55-09:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(11,'IndiGo','6E-6151',2,'10:00','11:20',0,NULL,'6E-6151 | HYD-MAA | 10:00-11:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(12,'IndiGo','6E-289',2,'11:45','13:05',0,NULL,'6E-289 | HYD-MAA | 11:45-13:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(13,'IndiGo','6E-514',2,'14:10','15:30',0,NULL,'6E-514 | HYD-MAA | 14:10-15:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(14,'Alliance Air','9I-893',2,'14:55','16:45',0,NULL,'9I-893 | HYD-MAA | 14:55-16:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(15,'IndiGo','6E-668',2,'16:20','17:40',0,NULL,'6E-668 | HYD-MAA | 16:20-17:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(16,'IndiGo','6E-598',2,'17:55','19:10',0,NULL,'6E-598 | HYD-MAA | 17:55-19:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(17,'IndiGo','6E-6486',2,'19:00','20:20',0,NULL,'6E-6486 | HYD-MAA | 19:00-20:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(18,'IndiGo','6E-6174',2,'20:40','22:05',0,NULL,'6E-6174 | HYD-MAA | 20:40-22:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(19,'IndiGo','6E-308',3,'09:55','11:00',0,NULL,'6E-308 | MAA-CJB | 09:55-11:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(20,'IndiGo','6E-881',3,'13:55','15:05',0,NULL,'6E-881 | MAA-CJB | 13:55-15:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(21,'IndiGo','6E-2123',3,'15:45','16:50',0,NULL,'6E-2123 | MAA-CJB | 15:45-16:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(22,'IndiGo','6E-726',3,'16:40','17:45',0,NULL,'6E-726 | MAA-CJB | 16:40-17:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(23,'IndiGo','6E-6812',3,'18:25','19:30',0,NULL,'6E-6812 | MAA-CJB | 18:25-19:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(24,'IndiGo','6E-731',3,'20:10','21:15',0,NULL,'6E-731 | MAA-CJB | 20:10-21:15','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(25,'IndiGo','6E-6181',4,'08:45','09:50',0,NULL,'6E-6181 | CJB-MAA | 08:45-09:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(26,'IndiGo','6E-6918',4,'11:30','12:35',0,NULL,'6E-6918 | CJB-MAA | 11:30-12:35','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(27,'IndiGo','6E-571',4,'15:40','16:45',0,NULL,'6E-571 | CJB-MAA | 15:40-16:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(28,'IndiGo','6E-2124',4,'17:25','18:30',0,NULL,'6E-2124 | CJB-MAA | 17:25-18:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(29,'IndiGo','6E-280',4,'18:15','19:20',0,NULL,'6E-280 | CJB-MAA | 18:15-19:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(30,'IndiGo','6E-659',4,'20:05','21:10',0,NULL,'6E-659 | CJB-MAA | 20:05-21:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(31,'IndiGo','6E-6315',4,'21:45','22:50',0,NULL,'6E-6315 | CJB-MAA | 21:45-22:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(32,'IndiGo','6E-6646',5,'07:25','08:55',0,NULL,'6E-6646 | HYD-CJB | 07:25-08:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(33,'IndiGo','6E-2584',5,'14:45','16:15',0,NULL,'6E-2584 | HYD-CJB | 14:45-16:15','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(34,'IndiGo','6E-6826',5,'16:00','17:30',0,NULL,'6E-6826 | HYD-CJB | 16:00-17:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(35,'IndiGo','6E-6308',5,'19:30','21:00',0,NULL,'6E-6308 | HYD-CJB | 19:30-21:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(36,'IndiGo','6E-467',6,'09:30','10:55',0,NULL,'6E-467 | CJB-HYD | 09:30-10:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(37,'IndiGo','6E-2585',6,'11:55','13:20',0,NULL,'6E-2585 | CJB-HYD | 11:55-13:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(38,'IndiGo','6E-6301',6,'18:05','19:30',0,NULL,'6E-6301 | CJB-HYD | 18:05-19:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(39,'IndiGo','6E-6307',6,'21:30','22:55',0,NULL,'6E-6307 | CJB-HYD | 21:30-22:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(40,'IndiGo','6E-253',7,'03:10','04:45',0,NULL,'6E-253 | MAA-PNQ | 03:10-04:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(41,'IndiGo','6E-607',7,'05:40','07:35',0,NULL,'6E-607 | MAA-PNQ | 05:40-07:35','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(42,'IndiGo','6E-502',7,'11:10','13:00',0,NULL,'6E-502 | MAA-PNQ | 11:10-13:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(43,'IndiGo','6E-183',7,'16:35','18:15',0,NULL,'6E-183 | MAA-PNQ | 16:35-18:15','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(44,'Air India Express','IX-2661',7,'22:05','23:45',0,NULL,'IX-2661 | MAA-PNQ | 22:05-23:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(45,'IndiGo','6E-561',7,'22:20','00:05',0,NULL,'6E-561 | MAA-PNQ | 22:20-00:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(46,'Air India Express','IX-2660',8,'00:15','02:10',0,NULL,'IX-2660 | PNQ-MAA | 00:15-02:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(47,'IndiGo','6E-6745',8,'00:45','02:35',0,NULL,'6E-6745 | PNQ-MAA | 00:45-02:35','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(48,'IndiGo','6E-159',8,'05:30','07:20',0,NULL,'6E-159 | PNQ-MAA | 05:30-07:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(49,'IndiGo','6E-625',8,'13:35','15:30',0,NULL,'6E-625 | PNQ-MAA | 13:35-15:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(50,'IndiGo','6E-6714',8,'19:00','20:45',0,NULL,'6E-6714 | PNQ-MAA | 19:00-20:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(51,'IndiGo','6E-238',8,'22:45','00:35',0,NULL,'6E-238 | PNQ-MAA | 22:45-00:35','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(52,'IndiGo','6E-337',9,'01:10','02:20',0,NULL,'6E-337 | HYD-PNQ | 01:10-02:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(53,'IndiGo','6E-351',9,'04:35','05:45',0,NULL,'6E-351 | HYD-PNQ | 04:35-05:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(54,'Star Air','S5-173',9,'06:45','07:55',0,NULL,'S5-173 | HYD-PNQ | 06:45-07:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(55,'IndiGo','6E-6471',9,'10:50','11:55',0,NULL,'6E-6471 | HYD-PNQ | 10:50-11:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(56,'IndiGo','6E-311',9,'12:50','14:10',0,NULL,'6E-311 | HYD-PNQ | 12:50-14:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(57,'IndiGo','6E-157',9,'14:40','15:45',0,NULL,'6E-157 | HYD-PNQ | 14:40-15:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(58,'IndiGo','6E-576',9,'16:15','17:55',0,NULL,'6E-576 | HYD-PNQ | 16:15-17:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(59,'IndiGo','6E-426',9,'20:20','21:30',0,NULL,'6E-426 | HYD-PNQ | 20:20-21:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(60,'IndiGo','6E-336',10,'01:50','03:10',0,NULL,'6E-336 | PNQ-HYD | 01:50-03:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(61,'IndiGo','6E-352',10,'06:25','07:50',0,NULL,'6E-352 | PNQ-HYD | 06:25-07:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(62,'IndiGo','6E-6473',10,'12:40','14:10',0,NULL,'6E-6473 | PNQ-HYD | 12:40-14:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(63,'IndiGo','6E-407',10,'14:50','16:05',0,NULL,'6E-407 | PNQ-HYD | 14:50-16:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(64,'IndiGo','6E-577',10,'18:30','19:50',0,NULL,'6E-577 | PNQ-HYD | 18:30-19:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(65,'Alliance Air','9I-868',10,'19:55','21:30',0,NULL,'9I-868 | PNQ-HYD | 19:55-21:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(66,'Star Air','S5-174',10,'20:35','21:45',0,NULL,'S5-174 | PNQ-HYD | 20:35-21:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(67,'IndiGo','6E-103',10,'22:20','23:40',0,NULL,'6E-103 | PNQ-HYD | 22:20-23:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(68,'IndiGo','6E-297',11,'14:15','16:10',0,NULL,'6E-297 | HYD-JDH | 14:15-16:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(69,'IndiGo','6E-814',12,'16:45','18:50',0,NULL,'6E-814 | JDH-HYD | 16:45-18:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(70,'IndiGo','6E-953',13,'02:45','05:40',0,NULL,'6E-953 | MAA-DEL | 02:45-05:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(71,'IndiGo','6E-937',13,'06:30','09:25',0,NULL,'6E-937 | MAA-DEL | 06:30-09:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(72,'Air India','AI-2832',13,'06:55','09:45',0,NULL,'AI-2832 | MAA-DEL | 06:55-09:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(73,'IndiGo','6E-404',13,'09:15','12:10',0,NULL,'6E-404 | MAA-DEL | 09:15-12:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(74,'IndiGo','6E-6002',13,'11:10','13:55',0,NULL,'6E-6002 | MAA-DEL | 11:10-13:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(75,'IndiGo','6E-939',13,'13:00','15:55',0,NULL,'6E-939 | MAA-DEL | 13:00-15:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(76,'IndiGo','6E-698',13,'16:45','19:40',0,NULL,'6E-698 | MAA-DEL | 16:45-19:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(77,'IndiGo','6E-613',13,'19:00','21:55',0,NULL,'6E-613 | MAA-DEL | 19:00-21:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(78,'IndiGo','6E-951',13,'21:50','00:35',0,NULL,'6E-951 | MAA-DEL | 21:50-00:35','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(79,'IndiGo','6E-2369',13,'23:45','02:40',0,NULL,'6E-2369 | MAA-DEL | 23:45-02:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(80,'IndiGo','6E-933',14,'02:55','05:45',0,NULL,'6E-933 | DEL-MAA | 02:55-05:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(81,'Air India','AI-2525',14,'06:00','08:40',0,NULL,'AI-2525 | DEL-MAA | 06:00-08:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(82,'IndiGo','6E-2368',14,'07:15','10:05',0,NULL,'6E-2368 | DEL-MAA | 07:15-10:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(83,'IndiGo','6E-940',14,'09:15','12:05',0,NULL,'6E-940 | DEL-MAA | 09:15-12:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(84,'Air India','AI-2467',14,'10:00','12:45',0,NULL,'AI-2467 | DEL-MAA | 10:00-12:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(85,'IndiGo','6E-434',14,'11:00','13:50',0,NULL,'6E-434 | DEL-MAA | 11:00-13:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(86,'IndiGo','6E-6115',14,'13:10','16:00',0,NULL,'6E-6115 | DEL-MAA | 13:10-16:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(87,'IndiGo','6E-6091',14,'15:15','18:05',0,NULL,'6E-6091 | DEL-MAA | 15:15-18:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(88,'IndiGo','6E-947',14,'16:40','19:30',0,NULL,'6E-947 | DEL-MAA | 16:40-19:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(89,'IndiGo','6E-950',14,'18:15','20:55',0,NULL,'6E-950 | DEL-MAA | 18:15-20:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(90,'IndiGo','6E-6341',14,'20:30','23:20',0,NULL,'6E-6341 | DEL-MAA | 20:30-23:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(91,'Air India','AI-2895',14,'21:55','00:40',0,NULL,'AI-2895 | DEL-MAA | 21:55-00:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(92,'Air India','AI-1806',15,'03:00','05:25',0,NULL,'AI-1806 | HYD-DEL | 03:00-05:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(93,'Akasa Air','QP-1405',15,'06:00','08:15',0,NULL,'QP-1405 | HYD-DEL | 06:00-08:15','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(94,'Air India','AI-2466',15,'08:35','10:55',0,NULL,'AI-2466 | HYD-DEL | 08:35-10:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(95,'IndiGo','6E-6202',15,'11:35','13:55',0,NULL,'6E-6202 | HYD-DEL | 11:35-13:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(96,'IndiGo','6E-711',15,'14:40','16:55',0,NULL,'6E-711 | HYD-DEL | 14:40-16:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(97,'Akasa Air','QP-1205',15,'17:25','19:55',0,NULL,'QP-1205 | HYD-DEL | 17:25-19:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(98,'IndiGo','6E-710',15,'17:45','20:10',0,NULL,'6E-710 | HYD-DEL | 17:45-20:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(99,'SpiceJet','SG-695',15,'19:30','21:55',0,NULL,'SG-695 | HYD-DEL | 19:30-21:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(100,'IndiGo','6E-890',15,'21:00','23:25',0,NULL,'6E-890 | HYD-DEL | 21:00-23:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(101,'Air India','AI-2870',15,'21:30','23:55',0,NULL,'AI-2870 | HYD-DEL | 21:30-23:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(102,'IndiGo','6E-6025',15,'23:50','02:05',0,NULL,'6E-6025 | HYD-DEL | 23:50-02:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(103,'IndiGo','6E-203',16,'07:10','09:20',0,NULL,'6E-203 | DEL-HYD | 07:10-09:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(104,'IndiGo','6E-705',16,'08:30','10:45',0,NULL,'6E-705 | DEL-HYD | 08:30-10:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(105,'Air India','AI-2899',16,'12:30','14:45',0,NULL,'AI-2899 | DEL-HYD | 12:30-14:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(106,'IndiGo','6E-6282',16,'13:30','15:45',0,NULL,'6E-6282 | DEL-HYD | 13:30-15:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(107,'Air India','AI-2465',16,'14:30','16:40',0,NULL,'AI-2465 | DEL-HYD | 14:30-16:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(108,'IndiGo','6E-6212',16,'14:50','17:05',0,NULL,'6E-6212 | DEL-HYD | 14:50-17:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(109,'IndiGo','6E-6217',16,'16:15','18:30',0,NULL,'6E-6217 | DEL-HYD | 16:15-18:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(110,'IndiGo','6E-382',16,'21:00','23:15',0,NULL,'6E-382 | DEL-HYD | 21:00-23:15','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(111,'SpiceJet','SG-696',16,'22:40','00:50',0,NULL,'SG-696 | DEL-HYD | 22:40-00:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(112,'IndiGo','6E-882',16,'23:15','01:30',0,NULL,'6E-882 | DEL-HYD | 23:15-01:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(113,'IndiGo','6E-6081',17,'07:05','08:05',0,NULL,'6E-6081 | MAA-BLR | 07:05-08:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(114,'IndiGo','6E-6892',17,'10:55','12:00',0,NULL,'6E-6892 | MAA-BLR | 10:55-12:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(115,'IndiGo','6E-6209',17,'14:35','15:40',0,NULL,'6E-6209 | MAA-BLR | 14:35-15:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(116,'IndiGo','6E-365',17,'17:45','18:45',0,NULL,'6E-365 | MAA-BLR | 17:45-18:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(117,'IndiGo','6E-796',17,'22:00','23:00',0,NULL,'6E-796 | MAA-BLR | 22:00-23:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(118,'IndiGo','6E-6269',18,'07:15','08:15',0,NULL,'6E-6269 | BLR-MAA | 07:15-08:15','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(119,'IndiGo','6E-904',18,'10:50','11:45',0,NULL,'6E-904 | BLR-MAA | 10:50-11:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(120,'IndiGo','6E-6208',18,'12:50','13:55',0,NULL,'6E-6208 | BLR-MAA | 12:50-13:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(121,'IndiGo','6E-413',19,'06:25','07:40',0,NULL,'6E-413 | HYD-BLR | 06:25-07:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(122,'Air India Express','IX-2019',19,'08:05','09:25',0,NULL,'IX-2019 | HYD-BLR | 08:05-09:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(123,'IndiGo','6E-6405',19,'14:30','15:50',0,NULL,'6E-6405 | HYD-BLR | 14:30-15:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(124,'IndiGo','6E-314',19,'16:30','17:50',0,NULL,'6E-314 | HYD-BLR | 16:30-17:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(125,'IndiGo','6E-6417',19,'17:45','19:05',0,NULL,'6E-6417 | HYD-BLR | 17:45-19:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(126,'IndiGo','6E-6783',19,'18:45','20:05',0,NULL,'6E-6783 | HYD-BLR | 18:45-20:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(127,'Air India Express','IX-2983',19,'19:20','20:45',0,NULL,'IX-2983 | HYD-BLR | 19:20-20:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(128,'Alliance Air','9I-518',19,'19:35','21:20',0,NULL,'9I-518 | HYD-BLR | 19:35-21:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(129,'IndiGo','6E-6505',19,'21:25','22:45',0,NULL,'6E-6505 | HYD-BLR | 21:25-22:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(130,'Alliance Air','9I-519',19,'23:05','01:00',0,NULL,'9I-519 | HYD-BLR | 23:05-01:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(131,'IndiGo','6E-6178',20,'06:25','07:35',0,NULL,'6E-6178 | BLR-HYD | 06:25-07:35','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(132,'IndiGo','6E-484',20,'08:15','09:30',0,NULL,'6E-484 | BLR-HYD | 08:15-09:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(133,'IndiGo','6E-6067',20,'16:30','17:45',0,NULL,'6E-6067 | BLR-HYD | 16:30-17:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(134,'IndiGo','6E-638',20,'18:35','19:45',0,NULL,'6E-638 | BLR-HYD | 18:35-19:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(135,'Air India Express','IX-1250',20,'19:15','20:25',0,NULL,'IX-1250 | BLR-HYD | 19:15-20:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(136,'Alliance Air','9I-517',20,'21:50','23:35',0,NULL,'9I-517 | BLR-HYD | 21:50-23:35','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(137,'Air India','AI-2828',21,'00:25','02:45',0,NULL,'AI-2828 | MAA-BOM | 00:25-02:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(138,'Air India','AI-2779',21,'01:30','03:40',0,NULL,'AI-2779 | MAA-BOM | 01:30-03:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(139,'IndiGo','6E-6140',21,'02:30','04:45',0,NULL,'6E-6140 | MAA-BOM | 02:30-04:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(140,'Air India','AI-2822',21,'04:05','06:25',0,NULL,'AI-2822 | MAA-BOM | 04:05-06:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(141,'Air India','AI-2740',21,'10:30','12:50',0,NULL,'AI-2740 | MAA-BOM | 10:30-12:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(142,'Air India','AI-640',21,'11:30','13:40',0,NULL,'AI-640 | MAA-BOM | 11:30-13:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(143,'IndiGo','6E-5371',21,'14:45','16:40',0,NULL,'6E-5371 | MAA-BOM | 14:45-16:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(144,'Air India','AI-436',21,'15:40','18:00',0,NULL,'AI-436 | MAA-BOM | 15:40-18:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(145,'Akasa Air','QP-1145',22,'05:00','07:00',0,NULL,'QP-1145 | BOM-MAA | 05:00-07:00','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(146,'IndiGo','6E-5188',22,'06:15','08:20',0,NULL,'6E-5188 | BOM-MAA | 06:15-08:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(147,'IndiGo','6E-5196',22,'15:25','17:30',0,NULL,'6E-5196 | BOM-MAA | 15:25-17:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(148,'IndiGo','6E-5105',22,'19:25','21:30',0,NULL,'6E-5105 | BOM-MAA | 19:25-21:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(149,'IndiGo','6E-6137',22,'23:00','01:05',0,NULL,'6E-6137 | BOM-MAA | 23:00-01:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(150,'Air India','AI-2872',23,'00:20','02:15',0,NULL,'AI-2872 | HYD-BOM | 00:20-02:15','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(151,'Air India','AI-2626',23,'01:55','03:45',0,NULL,'AI-2626 | HYD-BOM | 01:55-03:45','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(152,'IndiGo','6E-5245',23,'05:00','06:25',0,NULL,'6E-5245 | HYD-BOM | 05:00-06:25','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(153,'Air India','AI-418',23,'06:00','07:55',0,NULL,'AI-418 | HYD-BOM | 06:00-07:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(154,'IndiGo','6E-6196',23,'06:30','07:55',0,NULL,'6E-6196 | HYD-BOM | 06:30-07:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(155,'Air India','AI-2445',23,'08:55','10:55',0,NULL,'AI-2445 | HYD-BOM | 08:55-10:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(156,'Air India','AI-2447',23,'11:55','13:55',0,NULL,'AI-2447 | HYD-BOM | 11:55-13:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(157,'Alliance Air','9I-925',23,'22:15','00:30',0,NULL,'9I-925 | HYD-BOM | 22:15-00:30','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(158,'Air India','AI-2625',24,'00:30','02:05',0,NULL,'AI-2625 | BOM-HYD | 00:30-02:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(159,'Alliance Air','9I-926',24,'01:10','03:20',0,NULL,'9I-926 | BOM-HYD | 01:10-03:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(160,'IndiGo','6E-5179',24,'02:45','04:20',0,NULL,'6E-5179 | BOM-HYD | 02:45-04:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(161,'Air India','AI-417',24,'03:25','05:05',0,NULL,'AI-417 | BOM-HYD | 03:25-05:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(162,'Air India','AI-2444',24,'05:20','06:55',0,NULL,'AI-2444 | BOM-HYD | 05:20-06:55','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(163,'IndiGo','6E-6362',24,'05:45','07:20',0,NULL,'6E-6362 | BOM-HYD | 05:45-07:20','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(164,'IndiGo','6E-5246',24,'07:15','08:50',0,NULL,'6E-5246 | BOM-HYD | 07:15-08:50','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(165,'Air India','AI-2531',24,'09:35','11:05',0,NULL,'AI-2531 | BOM-HYD | 09:35-11:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(166,'IndiGo','6E-5318',24,'10:15','11:40',0,NULL,'6E-5318 | BOM-HYD | 10:15-11:40','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(167,'Air India','AI-2619',24,'12:30','14:05',0,NULL,'AI-2619 | BOM-HYD | 12:30-14:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(168,'Air India','AI-2875',24,'19:35','21:05',0,NULL,'AI-2875 | BOM-HYD | 19:35-21:05','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(169,'IndiGo (1 stop via HYD)','6E-372 + 6E-297',25,'09:55','16:10',1,'HYD','6E-372 + 6E-297 | MAA-JDH | 09:55-16:10','Imported from Excel reference master',1);
INSERT INTO "flights" VALUES(170,'IndiGo (1 stop via HYD)','6E-814 + 6E-6174',26,'16:45','22:05',1,'HYD','6E-814 + 6E-6174 | JDH-MAA | 16:45-22:05','Imported from Excel reference master',1);
CREATE TABLE hotel_options(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, is_active INTEGER NOT NULL DEFAULT 1);
INSERT INTO "hotel_options" VALUES(1,'Not Required',1);
INSERT INTO "hotel_options" VALUES(2,'To Confirm',1);
INSERT INTO "hotel_options" VALUES(3,'Required',1);
INSERT INTO "hotel_options" VALUES(4,'Booked',1);
INSERT INTO "hotel_options" VALUES(5,'Staying with Family',1);
INSERT INTO "hotel_options" VALUES(6,'Other / Manual',1);
CREATE TABLE routes(
 id INTEGER PRIMARY KEY AUTOINCREMENT,
 from_city_id INTEGER NOT NULL REFERENCES cities(id),
 to_city_id INTEGER NOT NULL REFERENCES cities(id),
 route_code TEXT NOT NULL COLLATE NOCASE UNIQUE,
 route_label TEXT NOT NULL,
 is_active INTEGER NOT NULL DEFAULT 1,
 UNIQUE(from_city_id,to_city_id)
);
INSERT INTO "routes" VALUES(1,2,1,'MAA-HYD','MAA → HYD',1);
INSERT INTO "routes" VALUES(2,1,2,'HYD-MAA','HYD → MAA',1);
INSERT INTO "routes" VALUES(3,2,3,'MAA-CJB','MAA → CJB',1);
INSERT INTO "routes" VALUES(4,3,2,'CJB-MAA','CJB → MAA',1);
INSERT INTO "routes" VALUES(5,1,3,'HYD-CJB','HYD → CJB',1);
INSERT INTO "routes" VALUES(6,3,1,'CJB-HYD','CJB → HYD',1);
INSERT INTO "routes" VALUES(7,2,7,'MAA-PNQ','MAA → PNQ',1);
INSERT INTO "routes" VALUES(8,7,2,'PNQ-MAA','PNQ → MAA',1);
INSERT INTO "routes" VALUES(9,1,7,'HYD-PNQ','HYD → PNQ',1);
INSERT INTO "routes" VALUES(10,7,1,'PNQ-HYD','PNQ → HYD',1);
INSERT INTO "routes" VALUES(11,1,8,'HYD-JDH','HYD → JDH',1);
INSERT INTO "routes" VALUES(12,8,1,'JDH-HYD','JDH → HYD',1);
INSERT INTO "routes" VALUES(13,2,5,'MAA-DEL','MAA → DEL',1);
INSERT INTO "routes" VALUES(14,5,2,'DEL-MAA','DEL → MAA',1);
INSERT INTO "routes" VALUES(15,1,5,'HYD-DEL','HYD → DEL',1);
INSERT INTO "routes" VALUES(16,5,1,'DEL-HYD','DEL → HYD',1);
INSERT INTO "routes" VALUES(17,2,4,'MAA-BLR','MAA → BLR',1);
INSERT INTO "routes" VALUES(18,4,2,'BLR-MAA','BLR → MAA',1);
INSERT INTO "routes" VALUES(19,1,4,'HYD-BLR','HYD → BLR',1);
INSERT INTO "routes" VALUES(20,4,1,'BLR-HYD','BLR → HYD',1);
INSERT INTO "routes" VALUES(21,2,6,'MAA-BOM','MAA → BOM',1);
INSERT INTO "routes" VALUES(22,6,2,'BOM-MAA','BOM → MAA',1);
INSERT INTO "routes" VALUES(23,1,6,'HYD-BOM','HYD → BOM',1);
INSERT INTO "routes" VALUES(24,6,1,'BOM-HYD','BOM → HYD',1);
INSERT INTO "routes" VALUES(25,2,8,'MAA-JDH','MAA → JDH',1);
INSERT INTO "routes" VALUES(26,8,2,'JDH-MAA','JDH → MAA',1);
CREATE TABLE status_options(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, next_action TEXT, sort_order INTEGER NOT NULL DEFAULT 0, is_active INTEGER NOT NULL DEFAULT 1);
INSERT INTO "status_options" VALUES(1,'Draft','Prepare trip details',10,1);
INSERT INTO "status_options" VALUES(2,'Awaiting Approval','Director approval',20,1);
INSERT INTO "status_options" VALUES(3,'Approved','Book confirmed travel',30,1);
INSERT INTO "status_options" VALUES(4,'Booking Pending','Complete booking',40,1);
INSERT INTO "status_options" VALUES(5,'Booked','Track travel / logistics',50,1);
INSERT INTO "status_options" VALUES(6,'Completed','Completed',60,1);
INSERT INTO "status_options" VALUES(7,'Cancelled','Cancelled',70,1);
CREATE TABLE transport_options(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, is_active INTEGER NOT NULL DEFAULT 1);
INSERT INTO "transport_options" VALUES(1,'Own Car',1);
INSERT INTO "transport_options" VALUES(2,'Rental Cab',1);
INSERT INTO "transport_options" VALUES(3,'Family Pickup',1);
INSERT INTO "transport_options" VALUES(4,'Hotel Pickup',1);
INSERT INTO "transport_options" VALUES(5,'Not Required',1);
INSERT INTO "transport_options" VALUES(6,'To Confirm',1);
INSERT INTO "transport_options" VALUES(7,'Other / Manual',1);
CREATE TABLE travellers(
 id INTEGER PRIMARY KEY AUTOINCREMENT,
 short_code TEXT NOT NULL COLLATE NOCASE UNIQUE,
 full_name TEXT,
 relation_group TEXT,
 is_active INTEGER NOT NULL DEFAULT 1
);
INSERT INTO "travellers" VALUES(1,'AS',NULL,'Family',1);
INSERT INTO "travellers" VALUES(2,'BB',NULL,'Family',1);
INSERT INTO "travellers" VALUES(3,'CA',NULL,'Family',1);
INSERT INTO "travellers" VALUES(4,'RJ',NULL,'Family',1);
INSERT INTO "travellers" VALUES(5,'JW',NULL,'Family',1);
INSERT INTO "travellers" VALUES(6,'PR',NULL,'Family',1);
INSERT INTO "travellers" VALUES(7,'MW',NULL,'Family',1);
CREATE TABLE trip_travellers(
 trip_id INTEGER NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
 traveller_id INTEGER NOT NULL REFERENCES travellers(id),
 PRIMARY KEY(trip_id, traveller_id)
);
INSERT INTO "trip_travellers" VALUES(1,1);
INSERT INTO "trip_travellers" VALUES(1,2);
INSERT INTO "trip_travellers" VALUES(1,3);
INSERT INTO "trip_travellers" VALUES(1,4);
INSERT INTO "trip_travellers" VALUES(2,5);
INSERT INTO "trip_travellers" VALUES(2,6);
INSERT INTO "trip_travellers" VALUES(2,7);
CREATE TABLE trip_types(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, is_active INTEGER NOT NULL DEFAULT 1);
INSERT INTO "trip_types" VALUES(1,'One Way',1);
INSERT INTO "trip_types" VALUES(2,'Round Trip',1);
CREATE TABLE trips(
 id INTEGER PRIMARY KEY AUTOINCREMENT,
 trip_code TEXT NOT NULL UNIQUE,
 departure_date TEXT NOT NULL,
 return_date TEXT,
 trip_type_id INTEGER REFERENCES trip_types(id),
 route_id INTEGER NOT NULL REFERENCES routes(id),
 onward_flight_id INTEGER REFERENCES flights(id),
 return_flight_id INTEGER REFERENCES flights(id),
 onward_pnr TEXT,
 onward_seats TEXT,
 return_pnr TEXT,
 return_seats TEXT,
 hotel_option_id INTEGER REFERENCES hotel_options(id),
 transport_option_id INTEGER REFERENCES transport_options(id),
 transport_details TEXT,
 total_cost REAL,
 status_option_id INTEGER REFERENCES status_options(id),
 remarks TEXT,
 created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
 updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO "trips" VALUES(1,'TR-2026-001','2026-11-05','2026-11-07',2,1,4,12,NULL,NULL,NULL,NULL,1,1,NULL,12449.0,2,NULL,'2026-09-23 02:55:43','2026-09-23 02:55:43');
INSERT INTO "trips" VALUES(2,'TR-2026-002','2026-11-07','2026-10-06',2,2,12,4,NULL,NULL,NULL,NULL,1,3,'Rajesh will pickup',12451.0,3,'Please verify return date.','2026-09-23 02:55:43','2026-09-23 02:55:43');
DELETE FROM "sqlite_sequence";
INSERT INTO "sqlite_sequence" VALUES('cities',8);
INSERT INTO "sqlite_sequence" VALUES('travellers',7);
INSERT INTO "sqlite_sequence" VALUES('routes',196);
INSERT INTO "sqlite_sequence" VALUES('transport_options',7);
INSERT INTO "sqlite_sequence" VALUES('status_options',7);
INSERT INTO "sqlite_sequence" VALUES('hotel_options',6);
INSERT INTO "sqlite_sequence" VALUES('trip_types',2);
INSERT INTO "sqlite_sequence" VALUES('flights',170);
INSERT INTO "sqlite_sequence" VALUES('trips',2);