CREATE TABLE "1.childpart_new" (
	"childpart_id"	INTEGER NOT NULL UNIQUE,
	"reference_number" TEXT NOT NULL UNIQUE,
	"child_part_number"	TEXT NOT NULL UNIQUE,
	"child_part_description"	TEXT NOT NULL,
	"default_qty"	INTEGER NOT NULL,
	"total_weight"	NUMERIC NOT NULL,
	"part_weight"	NUMERIC NOT NULL,
	"tol_percent"	NUMERIC NOT NULL,
	"tol_part_weight"	NUMERIC NOT NULL,
	"negative_tol_percent"	NUMERIC NOT NULL,
	"negative_tol_part_weight"	NUMERIC NOT NULL,
	"positive_tol_percent"	NUMERIC NOT NULL,
	"positive_tol_part_weight"	NUMERIC NOT NULL,
	"image"	BLOB,
	"expiry_days"	INTEGER NOT NULL,
	"batch_no"	TEXT,
	PRIMARY KEY("childpart_id" AUTOINCREMENT)
);


INSERT INTO "1.childpart_new" (
	childpart_id,
	reference_number,
	child_part_number,
	child_part_description,
	default_qty,
	total_weight,
	part_weight,
	tol_percent,
	tol_part_weight,
	negative_tol_percent,
	negative_tol_part_weight,
	positive_tol_percent,
	positive_tol_part_weight,
	image,
	expiry_days
	
)
SELECT 
	childpart_id,
	child_part_number,
	child_part_number,
	child_part_description,
	default_qty,
	total_weight,
	part_weight,
	tol_percent,
	tol_part_weight,
	negative_tol_percent,
	negative_tol_part_weight,
	positive_tol_percent,
	positive_tol_part_weight,
	image,
	365
FROM "1.childpart";

DROP TABLE "1.childpart";

ALTER TABLE "1.childpart_new" RENAME TO "1.childpart";



CREATE TABLE "4.childpart_history_new" (
	"childpart_history_id"	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	"date_time"	TEXT NOT NULL,
	"customer_name"	TEXT NOT NULL,
	"reference_number"	TEXT NOT NULL,
	"child_part_number"	TEXT NOT NULL,
	"child_part_description"	TEXT NOT NULL,
	"quantity"	INTEGER NOT NULL,
	"part_weight"	TEXT NOT NULL,
	"net_weight"	TEXT NOT NULL,
	"pallet_weight"	TEXT NOT NULL,
	"gross_weight"	TEXT NOT NULL,
	"batch_no"	TEXT NOT NULL,
	"other_info"	TEXT NOT NULL,
	"no_of_packet"	TEXT NOT NULL,
	"negative_tol_percent"	TEXT NOT NULL,
	"negative_tol_part_weight"	TEXT NOT NULL,
	"db_part_weight"	TEXT NOT NULL,
	"positive_tol_percent"	TEXT NOT NULL,
	"positive_tol_part_weight"	TEXT NOT NULL,
	"expiry_days"	INTEGER NOT NULL,
	"expiry_date"	TEXT NOT NULL
);

DROP TABLE "4.childpart_history";

ALTER TABLE "4.childpart_history_new" RENAME TO "4.childpart_history";
