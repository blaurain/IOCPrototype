package com.shephertz.app42.paas.customcode.sample;

import org.json.JSONException;
import org.json.JSONObject;

import com.shephertz.app42.paas.customcode.Executor;
import com.shephertz.app42.paas.customcode.HttpRequestObject;
import com.shephertz.app42.paas.customcode.HttpResponseObject;
import com.shephertz.app42.paas.sdk.java.ServiceAPI;
import com.shephertz.app42.paas.sdk.java.log.LogService;
import com.shephertz.app42.paas.sdk.java.user.User;  
import com.shephertz.app42.paas.sdk.java.user.UserService;  


public class MyCustomCode implements Executor {

	 private ServiceAPI sp = new ServiceAPI(	  	
			     "632e423ee26a2bd861bff64ab1bb57c698f32b517781f10b0f8a8703fdde74f4",
			      "07b133dbbbc60d682a6341b67fe1a134ac8e5ca91dd01b2f574807beee767cc0");
	 

	private final int HTTP_STATUS_SUCCESS = 200;

	private String moduleName = "App42CustomCodeTest";

	
	/** 
	 * Write your custom code inside this method 
	 */
	@Override
	public HttpResponseObject execute(HttpRequestObject request) {
		JSONObject body = request.getBody();

		// Build Log Service For logging in Your Code
		LogService logger = sp.buildLogService();
		logger.debug(" Recieved Request Body : :" + body.toString(), moduleName);

		// Write Your Custom Code Here
		// ......//
		
		
		
		String userName = "SMBUTTS";  
		String pwd = "********";  
		String emailId = "test@fake.com";
		UserService userService = sp.buildUserService();   
		User user = userService.createUser(userName, pwd, emailId);   
		String jsonResponse = user.toString();    

		logger.info("Running Custom Code Hello World  ", moduleName);
		
		// Create JSON Response Based on Your business logic
		JSONObject jsonResponseObj = new JSONObject();
		try {
			jsonResponseObj.put("name", "App42CustomCodeTest");
			jsonResponseObj.put("user", jsonResponse);
			//....//
			//....//
		} catch (JSONException e) {
			// Do exception Handling for JSON Parsing
		}
		// Return JSON Response and Status Code
		return new HttpResponseObject(HTTP_STATUS_SUCCESS, jsonResponseObj);
	}

}
