-- benchmark.lua
local token = ""
local counter = 0
local threads = {}

function setup(thread)
	thread:set("id", counter)
	table.insert(threads, thread)
	counter = counter + 1
end

token = "PasteYourBearerTokenHere"

local user_count = 10000 -- Simulate 10k unique users
local event_types = { "purchase", "login", "level_up", "quest_completed", "ads_watched" }
local operators = { ">", "<", "==", "!=" }
local fields = { "sum_value", "max_value", "min_value", "last_value", "first_value" }

request = function()
	local r = math.random(1, 100)
	local user_id = "user_" .. math.random(1, user_count)
	local event_name = event_types[math.random(1, #event_types)]

	local path = ""
	local method = "POST"
	local body = ""

	-- 70% Ingest: Massive variety of events and users
	if r <= 70 then
		path = "/events/ingest"
		local event_id = "bench_" .. math.random(1, 10000000)
		local val = math.random(1, 1000)
		body = string.format(
			[[{"event_id":"%s", "user_id":"%s", "event_name":"%s", "ts":%d, "value":%d}]],
			event_id,
			user_id,
			event_name,
			os.time(),
			val
		)

	-- 20% Evaluate: Varied expressions (testing the parser)
	elseif r <= 90 then
		path = "/events/evaluate"
		local field = fields[math.random(1, #fields)]
		local op = operators[math.random(1, #operators)]
		local threshold = math.random(1, 5000)
		local expression = string.format("%s.%s %s %d", event_name, field, op, threshold)

		body = string.format([[{"user_id":"%s", "expression":"%s"}]], user_id, expression)

	-- 10% Query: Specific aggregate lookups
	else
		path = string.format("/events/aggregate/%s/%s", user_id, event_name)
		method = "GET"
		body = ""
	end

	return wrk.format(method, path, {
		["Authorization"] = "Bearer " .. token,
		["Content-Type"] = "application/json",
	}, body)
end
