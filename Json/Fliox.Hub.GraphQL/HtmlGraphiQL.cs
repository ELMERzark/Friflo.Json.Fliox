// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class HtmlGraphiQL
    {
        internal static string Get(string dbName, string schemaName) {
            var relBase = "../graphiql";
            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8' />
  <meta name='robots' content='noindex' />
  <meta name='referrer' content='origin' />
  <meta name='viewport' content='width=device-width, initial-scale=1' />
  <title>{schemaName} · {dbName} - GraphQL</title>
  <style>
    body {{
      height: 100vh;
      margin: 0;
      overflow: hidden;
    }}
    #splash {{
      color: #333;
      display: flex;
      flex-direction: column;
      font-family: system, -apple-system, 'San Francisco', '.SFNSDisplay-Regular', 'Segoe UI', Segoe, 'Segoe WP', 'Helvetica Neue', helvetica, 'Lucida Grande', arial, sans-serif;
      height: 100vh;
      justify-content: center;
      text-align: center;
    }}
  </style>
  <link rel='icon' href='{relBase}/favicon.ico'>
  <link type='text/css' href='{relBase}/graphiql.css' rel='stylesheet' />
</head>
<body>
<div id='splash'>
  Loading&hellip;
</div>

<script src='{relBase}/es6-promise/es6-promise.min.js'></script>
<script src='{relBase}/react/react.min.js'></script>
<script src='{relBase}/react/react-dom.min.js'></script>
<script src='{relBase}/graphiql.min.js'></script>
<!-- <script src='{relBase}/graphql-path.js'></script> -->
<script>
  // Parse the search string to get url parameters.
  var search = window.location.search;
  var parameters = {{}};
  search.substr(1).split('&').forEach(function (entry) {{
    var eq = entry.indexOf('=');
    if (eq >= 0) {{
      parameters[decodeURIComponent(entry.slice(0, eq))] =
              decodeURIComponent(entry.slice(eq + 1));
    }}
  }});

  // if variables was provided, try to format it.
  if (parameters.variables) {{
    try {{
      parameters.variables =
              JSON.stringify(JSON.parse(parameters.variables), null, 2);
    }} catch (e) {{
      // Do nothing, we want to display the invalid JSON as a string, rather
      // than present an error.
    }}
  }}

  // When the query and variables string is edited, update the URL bar so
  // that it can be easily shared
  function onEditQuery(newQuery) {{
    parameters.query = newQuery;
    updateURL();
  }}
  function onEditVariables(newVariables) {{
    parameters.variables = newVariables;
    updateURL();
  }}
  function onEditOperationName(newOperationName) {{
    parameters.operationName = newOperationName;
    updateURL();
  }}
  function updateURL() {{
    var newSearch = '?' + Object.keys(parameters).filter(function (key) {{
      return Boolean(parameters[key]);
    }}).map(function (key) {{
      return encodeURIComponent(key) + '=' +
              encodeURIComponent(parameters[key]);
    }}).join('&');
    history.replaceState(null, null, newSearch);
  }}

  const graphqlPath = '{dbName}'

  function graphQLFetcher(graphQLParams) {{
    // This example expects a GraphQL server at the path /graphql.
    // Change this to point wherever you host your GraphQL server.
    return fetch(graphqlPath, {{
      method: 'post',
      headers: {{
        'Accept': 'application/json',
        'Content-Type': 'application/json'
      }},
      body: JSON.stringify(graphQLParams),
    }}).then(function (response) {{
      return response.text();
    }}).then(function (responseBody) {{
      try {{
        return JSON.parse(responseBody);
      }} catch (error) {{
        return responseBody;
      }}
    }});
  }}

  // Render <GraphiQL /> into the body.
  ReactDOM.render(
          React.createElement(GraphiQL, {{
            fetcher: graphQLFetcher,
            query: parameters.query,
            variables: parameters.variables,
            operationName: parameters.operationName,
            onEditQuery: onEditQuery,
            onEditVariables: onEditVariables,
            onEditOperationName: onEditOperationName
          }}),
          document.body,
  );
</script>
</body>
</html>
";
        }
    }
}