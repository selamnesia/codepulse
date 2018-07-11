/*
 * Copyright 2018 Secure Decisions, a division of Applied Visions, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * This material is based on research sponsored by the Department of Homeland
 * Security (DHS) Science and Technology Directorate, Cyber Security Division
 * (DHS S&T/CSD) via contract number HHSP233201600058C.
 */
package com.secdec.codepulse.data.bytecode.parse

import com.secdec.codepulse.parsers.java9
import com.secdec.codepulse.parsers.java9.{ Java9BaseListener, Java9Parser }

class MethodDeclarationListener(callback: (String, Int) => Unit) extends Java9BaseListener {
	override def enterMethodDeclaration(ctx: Java9Parser.MethodDeclarationContext) = {
		val name = ctx.getText()
		val start = ctx.start.getLine()

		callback(name, start)
	}
}
