package DB

import (
	"context"
	"fmt"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

var db *mongo.Database = nil

const AuthCollection = "authCard"
const VoteCollection = "votesCard"

func GetDataBase(name, collection string) *mongo.Collection {
	if db == nil {
		db = conectToDb(name)
		db.Client().Database(name).Collection(AuthCollection).DeleteMany(context.Background(), bson.M{})
		db.Client().Database(name).Collection(VoteCollection).DeleteMany(context.Background(), bson.M{})
	}
	return db.Collection(collection)
}

func conectToDb(name string) *mongo.Database {
	opts := options.Client().ApplyURI("mongodb://root:nice123@localhost:27017/?directConnection=true")
	client, err := mongo.Connect(context.Background(), opts)
	if err != nil {
		panic(err)
	}
	connect := client.Database(name)
	// Send a ping to confirm a successful connection
	if err := client.Database(name).RunCommand(context.Background(), bson.D{{"ping", 1}}).Err(); err != nil {
		panic(err)
	}
	fmt.Println("Pinged your deployment. You successfully connected to MongoDB!")
	return connect
}
