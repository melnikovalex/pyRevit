package persistence

import (
	"../cli"
)

// ErroCodes
// 0: All OK
// 1: No data to write
// 2: data is available but did not get pushed under dry run
// 3: headers are required
type Result struct {
	ResultCode int
	Message    string
}

type DatabaseConnection struct {
	Config *Config
}

type Connection interface {
	Write(*TelemetryRecord, *cli.Logger) (*Result, error)
	// Read([]TelemetryRecord, *cli.Logger) (*Result, error)
}

func NewConnection(dbcfg *Config) (Connection, error) {
	w := DatabaseConnection{
		Config: dbcfg,
	}
	if dbcfg.Backend == Postgres {
		return GenericSQLConnection{w}, nil
	} else if dbcfg.Backend == MongoDB {
		return MongoDBConnection{w}, nil
	} else if dbcfg.Backend == MySql {
		return GenericSQLConnection{w}, nil
	} else if dbcfg.Backend == MSSql {
		return GenericSQLConnection{w}, nil
	} else if dbcfg.Backend == Sqlite {
		return GenericSQLConnection{w}, nil
	}
	// ... other writers

	panic("should not get here")
}
