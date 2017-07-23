import os.path
import json
path = "C:/output"

result = []
c = 0

for filename in os.listdir(path):
	if not os.path.isfile(os.path.join(path, filename)):
		continue
	_, extension = os.path.splitext(filename)
	if not extension == ".json":
		continue
	with open(os.path.join(path, filename)) as file:
		jsonstring = file.read()
	data = json.loads(jsonstring)
	short_data = dict([
		("schwerp_x", data["schwerp_x"]),
		("schwerp_y", data["schwerp_y"]),
		("address", data["address"]),
		("filename", data["filename"])])
	result.append(short_data)
	c += 1
	if c % 100 == 0:
		print c

print "Writing..."
with open("metadata.json", "w") as output_file:
    output_file.write(json.dumps(dict([("buildings", result)])))